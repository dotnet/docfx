# JSON Model

Name | Description
-----| ----
_rel | The relative path of the root output folder from current output file. For example, if the output file is `a/b/c.html` from root output folder, then the value is `../../`.
_path | The path of current output file starting from root output folder.
_navPath | The relative path of the root TOC file from root output folder, if exists. The root TOC file stands for the TOC file in root output folder. For example, if the output file is html file, the value is `toc.html`.
_navRel | The relative path from current output file to the root TOC file, if exists. For example, if the root TOC file is `toc.html` from root output folder, the value is empty.
_navKey | The original file path of the root TOC file starting with `~/`. `~/` stands for the folder where `docfx.json` is in, for example, `~/toc.md`.
_tocPath | The relative path of the TOC file that current output file belongs to from root output folder, if current output file is in that TOC file. If current output file is not defined in any TOC file, the nearest TOC file is picked.
_tocRel | The relative path from current output file to its TOC file. For example, if the TOC file is `a/toc.html` from root output folder, the value is `../`.
_tocKey | The original file path of the TOC file starting with `~/`. `~/` stands for the folder where `docfx.json` is in, for example, `~/a/toc.yml`.

