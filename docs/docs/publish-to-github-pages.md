# Publish to GitHub Pages

## Configure CI build source link

Docfx uses the following environment variables to guess the branch name used for *Improve this doc* link or *View source* link and fallback to the `DOCFX_SOURCE_BRANCH_NAME` environment variable:

- `APPVEYOR_REPO_BRANCH` - [AppVeyor](https://www.appveyor.com/)
- `BUILD_SOURCEBRANCHNAME` - [Azure Pipelines](https://azure.microsoft.com/en-us/services/devops/pipelines/)
- `CI_BUILD_REF_NAME` - [GitLab CI](https://about.gitlab.com/gitlab-ci/)
- `Git_Branch` - [TeamCity](https://www.jetbrains.com/teamcity/)
- `GIT_BRANCH` - [Jenkins](https://jenkins.io/)
- `GIT_LOCAL_BRANCH` - [Jenkins](https://jenkins.io/)
