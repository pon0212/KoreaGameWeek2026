# GitHub Actions for Unity

The `Unity CI` workflow runs on pushes and pull requests targeting `main`. It checks the Unity project layout, checks changed files for whitespace errors, then opens the project with Unity Test Runner so script compilation and EditMode tests run on GitHub Actions. Test logs are uploaded as the `unity-test-results` artifact when available.

## One-time setup

Unity compilation and EditMode tests need a license on the hosted runner. Without it, static project checks still run and the Unity step is reported as skipped. To enable Unity checks, open **Settings → Secrets and variables → Actions → New repository secret** and add:

- Name: `UNITY_LICENSE`
- Value: the contents of the Unity `.ulf` license file for the account/build server authorized to run CI.

Then open **Actions → Unity CI → Run workflow** for a manual first run, or push a change under `Assets`, `Packages`, or `ProjectSettings`.

The workflow reads the Editor version from `ProjectSettings/ProjectVersion.txt`. It does not deploy or publish a game build.
