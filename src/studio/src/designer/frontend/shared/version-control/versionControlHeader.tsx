import { createTheme, createStyles, Grid, WithStyles, withStyles } from '@material-ui/core';
import axios, { CancelTokenSource } from 'axios';
import * as React from 'react';
import createVersionControlUrls, { IVersionControlUrls } from 'app-shared/utils/createVersionControlUrls';
import { get, post } from '../utils/networking';
import altinnTheme from '../theme/altinnStudioTheme';
import { getLanguageFromKey } from '../utils/language';
import postMessages from '../utils/postMessages';
import FetchChangesComponent from './fetchChanges';
import ShareChangesComponent from './shareChanges';
import CloneButton from './cloneButton';
import CloneModal from './cloneModal';
import SyncModalComponent, { ISyncModalComponentProps } from './syncModal';

export interface IVersionControlHeaderProps extends WithStyles<typeof styles> {
  language: any;
  org: string;
  repo: string;
}

export interface IVersionControlHeaderState {
  changesInMaster: boolean;
  changesInLocalRepo: boolean;
  hasPushRights: boolean;
  modalState: Partial<ISyncModalComponentProps>;
  mergeConflict: boolean;
  cloneModalAnchor?: Element;
}

const theme = createTheme(altinnTheme);

const styles = createStyles({
  headerStyling: {
    background: theme.altinnPalette.primary.greyLight,
    paddingTop: 10,
  },
});

const statusHasMergeConflict = (result: any) => {
  return result?.repositoryStatus === 'MergeConflict';
};

const forceRepoStatusCheck = () => {
  window.postMessage('forceRepoStatusCheck', window.location.href);
};

const initialModalState: Partial<ISyncModalComponentProps> = {
  anchorEl: null,
  header: '',
  descriptionText: [] as string[],
  isLoading: false,
  shouldShowDoneIcon: false,
  btnText: '',
  shouldShowCommitBox: false,
  btnClick: () => undefined as any,
};

class VersionControlHeader extends React.Component<IVersionControlHeaderProps, IVersionControlHeaderState> {
  constructor(_props: IVersionControlHeaderProps) {
    super(_props);
    this.state = {
      changesInMaster: false,
      changesInLocalRepo: false,
      hasPushRights: false,
      mergeConflict: false,
      modalState: initialModalState,
      cloneModalAnchor: null,
    };
    this.urls = createVersionControlUrls(this.props.org, this.props.repo);
    this.cancelTokenSource = axios.CancelToken.source();
  }

  public componentDidMount() {
    this.componentMounted = true;
    // check status every 5 min
    this.interval = window?.setInterval(() => this.updateStateOnIntervals(), 300000) || 0;
    this.getStatus()
      .then(this.getRepoPermissions)
      .then(this.updateState.bind(this));
    window.addEventListener('message', this.changeToRepoOccurred);
  }

  public componentWillUnmount() {
    clearInterval(this.interval);
    this.cancelTokenSource.cancel('ComponentWillUnmount'); // Cancel the getRepoPermissions() get request
    window.removeEventListener('message', this.changeToRepoOccurred);
  }

  async getStatus(): Promise<any> {
    try {
      const result = await this.fetchStatus();
      return {
        mergeConflict: statusHasMergeConflict(result),
        changesInMaster: result.behindBy !== 0,
        changesInLocalRepo: result.contentStatus
          .filter(({ fileStatus }: Partial<{ fileStatus: string }>) => fileStatus !== 'Ignored').length > 0,
      };
    } catch (err) {
      this.stopLoadingWhenLoadingFailed(err);
    }
    return undefined;
  }

  getRepoPermissions = async (prevStatus: any) => {
    let hasPushRights = false;
    try {
      const currentRepo = await get(this.urls.repository, { cancelToken: this.cancelTokenSource.token });
      hasPushRights = currentRepo.permissions.push;
    } catch (err) {
      if (axios.isCancel(err)) {
        // This is handy when debugging axios cancelations when unmounting
        // TODO: Fix other cancelations when unmounting in this component
        // console.info('Component did unmount. Get canceled.');
      } else {
        // TODO: Handle error
        console.error('getRepoPermissions failed', err);
      }
    }
    return {
      ...prevStatus,
      hasPushRights,
    };
  };

  cancelTokenSource: CancelTokenSource;

  interval: number;

  componentMounted = false;

  urls: IVersionControlUrls;

  public changeToRepoOccurred = (event: any) => {
    if (event.data === postMessages.filesAreSaved && this.componentMounted) {
      this.getStatus()
        .then(this.updateState.bind(this));
    }
  };

  closeSyncModal = () => {
    if (!this.state.mergeConflict) {
      this.setState((prev) => ({
        modalState: {
          ...prev.modalState,
          anchorEl: null,
        },
      }));
    }
  };

  fetchChanges = (currentTarget: Element) => {
    this.setState((prev) => ({
      modalState: {
        ...prev.modalState,
        anchorEl: currentTarget,
        header: getLanguageFromKey('sync_header.fetching_latest_version', this.props.language),
        isLoading: true,
      },
    }));

    get(this.urls.pull).then((result: any) => {
      if (this.componentMounted) {
        if (result.repositoryStatus === 'Ok') {
          // if pull was successfull, show app is updated message
          this.setState({
            changesInMaster: result.behindBy !== 0,
            changesInLocalRepo: result.contentStatus.length > 0,
            modalState: {
              header: getLanguageFromKey('sync_header.service_updated_to_latest', this.props.language),
              isLoading: false,
              shouldShowDoneIcon: true,
            },
          });
          // force refetch  files
          window.postMessage(postMessages.refetchFiles, window.location.href);
          forceRepoStatusCheck();
        } else if (result.repositoryStatus === 'CheckoutConflict') {
          // if pull gives merge conflict, show user needs to commit message
          this.setState({
            modalState: {
              header: getLanguageFromKey('sync_header.changes_made_samme_place_as_user', this.props.language),
              descriptionText:
                [
                  getLanguageFromKey('sync_header.changes_made_samme_place_submessage', this.props.language),
                  getLanguageFromKey('sync_header.changes_made_samme_place_subsubmessage', this.props.language),
                ],
              btnText: getLanguageFromKey('sync_header.fetch_changes_btn', this.props.language),
              shouldShowCommitBox: true,
              btnClick: this.commitChanges,
            },
          });
        }
      }
    })
      .catch(this.stopLoadingWhenLoadingFailed);
  };

  shareChanges = (currentTarget: any, showNothingToPush: boolean) => {
    const commonModalState = {
      ...initialModalState,
      anchorEl: currentTarget,
    };
    if (!this.state.hasPushRights) {
      // if user don't have push rights, show modal stating no access to share changes
      this.updateState({
        modalState: {
          ...commonModalState,
          header: getLanguageFromKey('sync_header.sharing_changes_no_access', this.props.language),
          // eslint-disable-next-line max-len
          descriptionText: [getLanguageFromKey(
            'sync_header.sharing_changes_no_access_submessage', this.props.language,
          )],
        },
      });
    } else if (showNothingToPush) {
      this.updateState({
        modalState: {
          ...commonModalState,
          shouldShowDoneIcon: true,
          header: getLanguageFromKey('sync_header.nothing_to_push', this.props.language),
        },
      });
    } else {
      this.updateState({
        modalState: {
          ...commonModalState,
          header: getLanguageFromKey('sync_header.controlling_service_status', this.props.language),
          isLoading: true,
        },
      });
      this.fetchStatus()
        .then((result: any) => {
          commonModalState.isLoading = false;
          if (result) {
            // if user is ahead with no changes to commit, show share changes modal
            if (result.aheadBy > 0 && result.contentStatus.length === 0) {
              this.updateState({
                modalState: {
                  ...commonModalState,
                  header: getLanguageFromKey('sync_header.validation_completed', this.props.language),
                  btnText: getLanguageFromKey('sync_header.share_changes', this.props.language),
                  shouldShowCommitBox: false,
                  shouldShowDoneIcon: true,
                  btnClick: this.pushChanges,
                },
              });
            } else {
              // if user has changes to share, show write commit message modal
              this.updateState({
                modalState: {
                  ...commonModalState,
                  header: getLanguageFromKey('sync_header.describe_and_validate', this.props.language),
                  descriptionText:
                    [
                      getLanguageFromKey('sync_header.describe_and_validate_submessage', this.props.language),
                      getLanguageFromKey('sync_header.describe_and_validate_subsubmessage', this.props.language),
                    ],
                  btnText: getLanguageFromKey('sync_header.describe_and_validate_btnText', this.props.language),
                  shouldShowCommitBox: true,
                  btnClick: this.commitChanges,
                },
              });
            }
          }
        });
    }
  };

  pushChanges = () => {
    this.setState((prev) => ({
      modalState: {
        ...prev.modalState,
        header: getLanguageFromKey('sync_header.sharing_changes', this.props.language),
        isLoading: true,
      },
    }));

    post(this.urls.push).then((result: any) => {
      if (this.componentMounted) {
        if (result.isSuccessStatusCode) {
          this.setState({
            changesInMaster: false,
            changesInLocalRepo: false,
            modalState: {
              header: getLanguageFromKey('sync_header.sharing_changes_completed', this.props.language),
              descriptionText:
                [getLanguageFromKey('sync_header.sharing_changes_completed_submessage', this.props.language)],
              shouldShowDoneIcon: true,
            },
          });
        } else {
          // will be handled by error handling in catch
          throw new Error('Push failed');
        }
      }
    })
      .catch(this.stopLoadingWhenLoadingFailed);
    forceRepoStatusCheck();
  };

  commitChanges = (commitMessage: string) => {
    this.setState({
      modalState: {
        ...initialModalState,
        header: getLanguageFromKey('sync_header.validating_changes', this.props.language),
        descriptionText: [],
        isLoading: true,
      },
    });

    const options = {
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
      },
    };
    const bodyData = JSON.stringify({
      message: commitMessage, org: this.props.org, repository: this.props.repo,
    });
    const urls = this.urls;
    post(urls.commit, bodyData, options).then(() => {
      get(urls.pull).then((result: any) => {
        if (this.componentMounted) {
          // if pull was successfull, show app updated message
          if (result.repositoryStatus === 'Ok') {
            this.setState({
              modalState: {
                header: getLanguageFromKey('sync_header.validation_completed', this.props.language),
                descriptionText: [],
                shouldShowDoneIcon: true,
                btnText: getLanguageFromKey('sync_header.share_changes', this.props.language),
                btnClick: this.pushChanges,
              },
            });
          } else if (result.repositoryStatus === 'MergeConflict') {
            // if pull resulted in a mergeconflict, show mergeconflict message
            this.setState({
              mergeConflict: true,
              modalState: {
                header: getLanguageFromKey('sync_header.merge_conflict_occured', this.props.language),
                descriptionText: [getLanguageFromKey(
                  'sync_header.merge_conflict_occured_submessage', this.props.language,
                )],
                btnText: getLanguageFromKey('sync_header.merge_conflict_btn', this.props.language),
                btnClick: forceRepoStatusCheck,
              },
            });
          }
        }
      })
        .catch(this.stopLoadingWhenLoadingFailed);
    })
      .catch(this.stopLoadingWhenLoadingFailed);
  };

  closeCloneModal = () => {
    this.setState({
      cloneModalAnchor: null,
    });
  };

  public openCloneModal = (event: React.MouseEvent) => {
    this.setState({
      cloneModalAnchor: event.currentTarget,
    });
  };

  public async fetchStatus() {
    return get(this.urls.status);
  }

  public updateState(state: any) {
    if (state && Object.keys(state).length) {
      this.setState((prev: any) => ({
        ...prev, ...state,
      }));
    }
  }

  public updateStateOnIntervals() {
    if (this.componentMounted) {
      this.getStatus().then(this.updateState.bind(this));
    }
  }

  public stopLoadingWhenLoadingFailed(err: any) {
    console.error('Failed to load in version control header', this.state.modalState.isLoading, err);
    if (this.state.modalState.isLoading) {
      this.setState(() => ({
        modalState: {
          header: getLanguageFromKey('sync_header.repo_is_offline', this.props.language),
          isLoading: false,
        },
      }));
    }
  }

  public render() {
    const { classes } = this.props;
    return (
      <Grid
        container
        direction='row'
        className={classes.headerStyling}
        justify='flex-start'
      >
        <Grid item={true} style={{ marginRight: '24px' }}>
          <CloneButton
            onClick={this.openCloneModal}
            buttonText={getLanguageFromKey('sync_header.clone', this.props.language)}
          />
        </Grid>
        <Grid item={true} style={{ marginRight: '24px' }}>
          <FetchChangesComponent
            changesInMaster={this.state.changesInMaster}
            fetchChanges={this.fetchChanges}
            language={this.props.language}
          />
        </Grid>
        <Grid item={true}>
          <ShareChangesComponent
            changesInLocalRepo={this.state.changesInLocalRepo}
            hasMergeConflict={this.state.mergeConflict}
            language={this.props.language}
            shareChanges={this.shareChanges}
          />
        </Grid>

        <SyncModalComponent
          { ...this.state.modalState }
          handleClose={this.closeSyncModal}
        />
        <CloneModal
          anchorEl={this.state.cloneModalAnchor}
          onClose={this.closeCloneModal}
          language={this.props.language}
        />
      </Grid>
    );
  }
}

export default withStyles(styles)(VersionControlHeader);
