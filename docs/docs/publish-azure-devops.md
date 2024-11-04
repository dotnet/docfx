# Deploy to GitHub Pages from Azure DevOps


### 1. **Create a GitHub Personal Access Token (PAT)**
You will need a GitHub PAT with repo permissions to allow Azure DevOps to push to the GitHub Pages branch.

- Go to your GitHub account > **Settings** > **Developer settings** > **Personal Access Tokens**.
- Click on **Generate new token** and select the required repo permissions.
- Save the token somewhere secure as it will be used in the Azure DevOps pipeline.

### 2. **Create Azure DevOps Pipeline**

#### a. **Setup DocFX**
Ensure that you have a `docfx.json` configuration file in your repository. This will define how DocFX generates your documentation.

- Install DocFX locally on your development machine and create the configuration:

  ```bash
  docfx init -q
  ```

This will generate the `docfx.json` file, which you can configure to specify input and output directories, build settings, etc.

#### b. **Azure Pipelines YAML File**

Create an Azure DevOps pipeline YAML file (e.g., `azure-pipelines.yml`) in the root of your project to automate the build and deployment.

Here is a sample pipeline YAML configuration for DocFX:

```yaml
trigger:
  branches:
    include:
      - main  # Trigger on main branch

pool:
  vmImage: 'ubuntu-latest'  # Use the latest Ubuntu VM

variables:
  - group: GitHubPAT

steps:
  # Step 1: Install DocFX
  - task: UseDotNet@2
    inputs:
      packageType: 'sdk'
      version: '8.x'  # Ensure you have .NET SDK installed
      installationPath: $(Agent.ToolsDirectory)/dotnet

  - script: |
      dotnet tool install -g docfx
    displayName: 'Install DocFX'

  # Step 2: Build the Documentation using DocFX
  - script: |
      docfx build
    displayName: 'Build Documentation'

  # Step 3: Deploy to GitHub Pages
  - task: Bash@3
    displayName: 'Deploy to GitHub Pages'
    inputs:
      targetType: 'inline'
      script: |
        git config --global user.email "your-email@example.com"
        git config --global user.name "Your Name"
        git clone --branch gh-pages https://$GITHUB_PAT@github.com/$GITHUB_REPO.git out
        rm -rf out/*
        cp -r _site/* out/
        cd out
        git add --all
        git commit -m "Update documentation"
        git push origin gh-pages
    env:
      GITHUB_PAT: $(GITHUB_PAT)  # The GitHub Personal Access Token
      GITHUB_REPO: your-github-username/your-repo-name  # Update with your repo details
```

#### c. **Setup Azure Pipeline Variables**

- Go to **Azure DevOps** > **Pipelines** > **Library**.
- Create a new pipeline variable group called GitHubPAT.
- Add a new variable named GITHUB_PAT and set the value to the GitHub Personal Access Token you created earlier. Mark this variable as secret.

### 3. **Configure GitHub Pages**

- Go to your GitHub repository.
- Under **Settings** > **Pages**, set the source branch to gh-pages.

### 4. **Run the Pipeline**

- Commit the azure-pipelines.yml file to your repository and trigger the pipeline.
- Azure DevOps will now:
  1. Install DocFX.
  2. Build the documentation.
  3. Deploy the documentation to the gh-pages branch in GitHub.

### 5. **Access the Documentation**

After the pipeline finishes, your documentation will be available in the github-pages deployments.