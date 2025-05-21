# llms.txt

DocFX can generate a `llms.txt` file for your documentation site based on the [llms.txt standard](https://github.com/AnswerDotAI/llms-txt). This file helps LLMs (Large Language Models) better understand and navigate your documentation without loading the entire site.

## Configuration

To enable llms.txt generation, add the `llmsText` section to your `docfx.json` configuration file under the `build` section:

```json
{
  "build": {
    "llmsText": {
      "title": "My Project Documentation",
      "summary": "A brief description of your project",
      "details": "More detailed information about your project and its documentation",
      "sections": {
        "Main": [
          {
            "title": "Getting Started",
            "url": "docs/getting-started.md",
            "description": "How to get started with the project"
          },
          {
            "title": "API Reference",
            "url": "docs/api-reference.html"
          }
        ],
        "Optional": [
          {
            "title": "Advanced Configuration",
            "url": "docs/advanced-config.md",
            "description": "Advanced configuration options"
          }
        ]
      }
    }
  }
}
```

### Properties

The `llmsText` section supports the following properties:

- `title` (required): The name of your project, displayed as an H1 heading
- `summary`: A brief description of your project, displayed as a blockquote
- `details`: Additional information about the project, displayed as regular text
- `sections`: A dictionary of section names to lists of links

Each link in a section contains:

- `title`: The title of the link
- `url`: The URL of the link (can be relative or absolute)
- `description` (optional): Additional description of the link

## Output Format

The generated `llms.txt` file follows the [llms.txt standard](https://github.com/AnswerDotAI/llms-txt) format:

```markdown
# My Project Documentation

> A brief description of your project

More detailed information about your project and its documentation

## Main

- [Getting Started](docs/getting-started.md): How to get started with the project
- [API Reference](docs/api-reference.html)

## Optional

- [Advanced Configuration](docs/advanced-config.md): Advanced configuration options
```

## Integration

To enable llms.txt generation for your documentation, add `LlmsTextGenerator` to your post-processors:

```json
{
  "build": {
    "postProcessors": ["LlmsTextGenerator"]
  }
}
```

The llms.txt file will be generated in the root of your documentation output folder.