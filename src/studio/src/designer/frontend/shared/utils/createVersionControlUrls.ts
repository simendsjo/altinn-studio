export interface IVersionControlUrls {
  repository: string,
  status: string,
  latestCommit: string,
  pull: string,
  push: string,
  commit: string,
}

const createVersionControlUrls = (org: string, repo: string) : IVersionControlUrls => {
  const origin = window.location.origin;
  return {
    repository: `${origin}/designerapi/Repository/GetRepository?org=${org}&repository=${repo}`,
    status: `${origin}/designerapi/Repository/RepoStatus?org=${org}&repository=${repo}`,
    latestCommit: `${origin}/designerapi/Repository/GetLatestCommitFromCurrentUser?org=${org}&repository=${repo}`,
    pull: `${origin}/designerapi/Repository/Pull?org=${org}&repository=${repo}`,
    push: `${origin}/designerapi/Repository/Push?org=${org}&repository=${repo}`,
    commit: `${origin}/designerapi/Repository/Commit`,
  };
};

export default createVersionControlUrls;
