# Contributing to DocFX
One of the easiest ways to contribute is to participate in discussions and discuss issues. You can also contribute by submitting pull requests with code changes into **`dev`** branch.
This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community.
For more information see the [.NET Foundation Code of Conduct](http://www.dotnetfoundation.org/code-of-conduct).

## Filing issues
When filing issues, please use our [bug filing templates](ISSUE_TEMPLATE.md).
The best way to get your bug fixed is to be as detailed as you can be about the problem.
Providing a minimal project with steps to reproduce the problem is ideal.
Here are questions you can answer before you file a bug to make sure you're not missing any important information.

1. Did you include the snippet of broken code in the issue?
2. What are the *EXACT* steps to reproduce this problem?

GitHub supports [markdown](https://guides.github.com/features/mastering-markdown/), so when filing bugs make sure you check the formatting before clicking submit.

## Issue Maintenance and Closure
* If an issue is inactive for 90 days (no activity of any kind), it will be marked for closure with `stale`.
* If after this label is applied, no further activity occurs in the next 7 days, the issue will be closed.
  * If an issue has been closed and you still feel it's relevant, feel free to ping a maintainer or add a comment!

## Contributing code and content
You will need to sign a [Contributor License Agreement](https://cla.dotnetfoundation.org/) before submitting your pull request. To complete the Contributor License Agreement (CLA), you will need to submit a request via the form and then electronically sign the Contributor License Agreement when you receive the email containing the link to the document. This needs to only be done once for any .NET Foundation OSS project.

Per [roadmap](../roadmap.md), we are happy to receive any changes including improvements, new features or bug fixes in both `dev` and `v3` branch. If you are willing to contribute refactor or code cleanup, `v3` would be a more valuable target than `dev` in long run.

Here's a few things you should always do when making changes to the code base:

**Engineering guidelines**

The coding, style, and general engineering guidelines are published on the [Engineering guidelines](http://dotnet.github.io/docfx/guideline/engineering_guidelines.html) page.

**Commit/Pull Request Format**

```
Summary of the changes (Less than 80 chars)
 - Detail 1
 - Detail 2

#bugnumber (in this specific format)
```

**Tests**

-  Tests need to be provided for every bug/feature that is completed.
-  Tests only need to be present for issues that need to be verified by QA (e.g. not tasks)
-  If there is a scenario that is far too hard to test there does not need to be a test for it.
  - "Too hard" is determined by the team as a whole.
