# Localization Support

## Description
DocFX build supports localization contents, there are a few features need to be supported for localization contents:
  - Partial contents: the localization contents are often a part of source content.
  - Localization contents are not required to be stored in the same repo of source content, they can be everywhere.
  - Localization publishing may have a few specific requirements which are different with source publishing, they need overwrite the source configuration

## Design
- Treats localization content as the replacing content of source content, they are not independent repo for publishing but only stores the corresponding loc content.
- Localization publishing mixes the loc content and source content, loc content has higher priority to replace the source content.
- Localization publishing uses source configuration and localization overwrite configuration


## Workflow
![Localization Publishing And Build](resource/loc_build_publish.PNG)
