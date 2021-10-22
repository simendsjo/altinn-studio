import { createTheme, createStyles, Grid, WithStyles, withStyles } from '@material-ui/core';
import axios from 'axios';
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
import SyncModalComponent from './syncModal';

export interface IVersionControlHeaderProps extends WithStyles<typeof styles> {
  language: any;
  org: string;
  repo: string;
}

export interface IVersionControlHeaderState {
  changesInMaster: boolean;
  changesInLocalRepo: boolean;
  moreThanAnHourSinceLastPush: boolean;
  hasPushRight: boolean;
  anchorEl: any;
  modalState: any;
  mergeConflict: boolean;
  cloneModalOpen: boolean;
  cloneModalAnchor: any;
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

const initialModalState = {
  header: '',
  descriptionText: [] as string[],
  isLoading: '',
  shouldShowDoneIcon: false,
  btnText: '',
  shouldShowCommitBox: false,
  btnMethod: '',
};

class VersionControlHeader extends React.Component<IVersionControlHeaderProps, IVersionControlHeaderState> {
  public cancelToken = axios.CancelToken;

  public source = this.cancelToken.source();

  public interval: any;

  public componentIsMounted = false;

  public timeout: any;

  public urls: IVersionControlUrls;

  constructor(_props: IVersionControlHeaderProps) {
    super(_props);
    this.state = {
      changesInMaster: false,
      changesInLocalRepo: false,
      moreThanAnHourSinceLastPush: false,
      hasPushRight: null,
      anchorEl: null,
      mergeConflict: false,
      modalState: initialModalState,
      cloneModalOpen: false,
      cloneModalAnchor: null,
    };
    this.urls = createVersionControlUrls(this.props.org, this.props.repo);
  }

  public componentDidMount() {
    this.componentIsMounted = true;
    // check status every 5 min
    this.interval = setInterval(() => this.updateStateOnIntervals(), 300000);
    this.getStatus()
      .then(this.getRepoPermissions)
      .then(this.fetchLastCommit)
      .then(this.updateState);
    window.addEventListener('message', this.changeToRepoOccurred);
  }

  public componentWillUnmount() {
    clearInterval(this.interval);
    this.source.cancel('ComponentWillUnmount'); // Cancel the getRepoPermissions() get request
    clearTimeout(this.timeout);
    window.removeEventListener('message', this.changeToRepoOccurred);
  }

  public async getStatus(): Promise<any> {
    try {
      const result = await this.fetchStatus();
      return {
        mergeConflict: statusHasMergeConflict(result),
        changesInMaster: result.behindBy !== 0,
        changesInLocalRepo: result.contentStatus.length > 0,
      };
    } catch (err) {
      this.stopLoadingWhenLoadingFailed(err);
    }
    return undefined;
  }

  public getRepoPermissions = async (prevStatus: any) => {
    let hasPushRight = false;
    const url = this.urls.repository;
    try {
      const currentRepo = await get(url, { cancelToken: this.source.token });
      hasPushRight = currentRepo.permissions.push;
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
      hasPushRight,
    };
  };

  public changeToRepoOccurred = (event: any) => {
    if (event.data === postMessages.filesAreSaved && this.componentIsMounted) {
      this.getStatus()
        .then(this.updateState);
    }
  }

  public handleClose = () => {
    if (!this.state.mergeConflict) {
      this.setState({
        anchorEl: null,
      });
    }
  }

  public fetchChanges = (currentTarget: any) => {
    this.setState({
      anchorEl: currentTarget,
      modalState: {
        header: getLanguageFromKey('sync_header.fetching_latest_version', this.props.language),
        isLoading: true,
      },
    });

    const url = this.urls.pull;

    get(url).then((result: any) => {
      if (this.componentIsMounted) {
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
          this.forceRepoStatusCheck();
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
              btnMethod: this.commitChanges,
            },
          });
        }
      }
    })
      .catch(this.stopLoadingWhenLoadingFailed);
  }

  public shareChanges = (currentTarget: any, showNothingToPush: boolean) => {
    const newState: {
      anchorEl: any,
      modalState?: any,
    } = {
      anchorEl: currentTarget,
    };
    if (showNothingToPush) {
      newState.modalState = {
        shouldShowDoneIcon: true,
        header: getLanguageFromKey('sync_header.nothing_to_push', this.props.language),
      };
    }
    if (this.state.hasPushRight === true) {
      newState.modalState = {
        header: getLanguageFromKey('sync_header.controlling_service_status', this.props.language),
        isLoading: true,
      };
      this.fetchStatus()
        .then((result: any) => {
          if (result) {
            // if user is ahead with no changes to commit, show share changes modal
            if (result.aheadBy > 0 && result.contentStatus.length === 0) {
              newState.modalState = {
                header: getLanguageFromKey('sync_header.validation_completed', this.props.language),
                btnText: getLanguageFromKey('sync_header.share_changes', this.props.language),
                shouldShowDoneIcon: true,
                isLoading: false,
                btnMethod: this.pushChanges,
              };
            } else {
              // if user has changes to share, show write commit message modal
              newState.modalState = {
                header: getLanguageFromKey('sync_header.describe_and_validate', this.props.language),
                descriptionText:
                  [
                    getLanguageFromKey('sync_header.describe_and_validate_submessage', this.props.language),
                    getLanguageFromKey('sync_header.describe_and_validate_subsubmessage', this.props.language),
                  ],
                btnText: getLanguageFromKey('sync_header.describe_and_validate_btnText', this.props.language),
                shouldShowCommitBox: true,
                isLoading: false,
                btnMethod: this.commitChanges,
              };
            }
          }
        });
    } else if (!this.state.hasPushRight) {
      // if user don't have push rights, show modal stating no access to share changes
      newState.modalState = {
        header: getLanguageFromKey('sync_header.sharing_changes_no_access', this.props.language),
        // eslint-disable-next-line max-len
        descriptionText: [getLanguageFromKey(
          'sync_header.sharing_changes_no_access_submessage', this.props.language,
        )],
      };
    }
    this.updateState(newState);
  }

  public pushChanges = () => {
    this.setState({
      modalState: {
        header: getLanguageFromKey('sync_header.sharing_changes', this.props.language),
        isLoading: true,
      },
    });

    const url = this.urls.push;

    post(url).then((result: any) => {
      if (this.componentIsMounted) {
        if (result.isSuccessStatusCode) {
          this.setState({
            changesInMaster: false,
            changesInLocalRepo: false,
            moreThanAnHourSinceLastPush: true,
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
    this.forceRepoStatusCheck();
  }

  public commitChanges = (commitMessage: string) => {
    this.setState({
      modalState: {
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
        if (this.componentIsMounted) {
          // if pull was successfull, show app updated message
          if (result.repositoryStatus === 'Ok') {
            this.setState({
              modalState: {
                header: getLanguageFromKey('sync_header.validation_completed', this.props.language),
                descriptionText: [],
                btnText: getLanguageFromKey('sync_header.share_changes', this.props.language),
                shouldShowDoneIcon: true,
                btnMethod: this.pushChanges,
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
                btnMethod: this.forceRepoStatusCheck,
              },
            });
          }
        }
      })
        .catch(this.stopLoadingWhenLoadingFailed);
    })
      .catch(this.stopLoadingWhenLoadingFailed);
  }

  public forceRepoStatusCheck = () => {
    window.postMessage('forceRepoStatusCheck', window.location.href);
  }

  public renderSyncModalComponent = () => {
    return (
      <SyncModalComponent
        anchorEl={this.state.anchorEl}
        header={this.state.modalState.header}
        descriptionText={this.state.modalState.descriptionText}
        isLoading={this.state.modalState.isLoading}
        shouldShowDoneIcon={this.state.modalState.shouldShowDoneIcon}
        btnText={this.state.modalState.btnText}
        shouldShowCommitBox={this.state.modalState.shouldShowCommitBox}
        handleClose={this.handleClose}
        btnClick={this.state.modalState.btnMethod}
      />
    );
  }

  public closeCloneModal = () => {
    this.setState({
      cloneModalOpen: false,
    });
  }

  public renderCloneModal = () => {
    return (
      <CloneModal
        anchorEl={this.state.cloneModalAnchor}
        open={this.state.cloneModalOpen}
        onClose={this.closeCloneModal}
        language={this.props.language}
      />
    );
  }

  public openCloneModal = (event: React.MouseEvent) => {
    this.setState({
      cloneModalOpen: true,
      cloneModalAnchor: event.currentTarget,
    });
  }

  public async fetchLastCommit(state: any) {
    if (!this.state.moreThanAnHourSinceLastPush) {
      const result = await get(this.urls.latestCommit);
      if (result) {
        const diff = new Date().getTime() - new Date(result.comitter.when).getTime();
        const oneHour = 60 * 60 * 1000;
        return {
          ...state,
          moreThanAnHourSinceLastPush: oneHour < diff,
        };
      }
    }
    return state;
  }

  public async fetchStatus() {
    return get(this.urls.status);
  }

  private updateState(state: any) {
    if (state && Object.keys(state).length) {
      this.setState((prev: any) => ({
        ...prev, ...state,
      }));
    }
  }

  public updateStateOnIntervals() {
    this.getStatus().then(this.fetchLastCommit).then(this.updateState);
  }

  private stopLoadingWhenLoadingFailed(err: any) {
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
            hasPushRight={this.state.hasPushRight}
            language={this.props.language}
            moreThanAnHourSinceLastPush={this.state.moreThanAnHourSinceLastPush}
            shareChanges={this.shareChanges}
          />
        </Grid>
        {this.renderSyncModalComponent()}
        {this.renderCloneModal()}
      </Grid>
    );
  }
}

export default withStyles(styles)(VersionControlHeader);
