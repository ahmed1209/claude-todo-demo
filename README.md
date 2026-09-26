# claude-todo-demo

## Parametra website

`docs/index.html` is a single-file website for the Parametra catalogue: Revit plugins, parametric families, Revit files and courses. It has no build step and no dependencies beyond Google Fonts.

**Preview locally:** open `docs/index.html` in a browser.

**Publish with GitHub Pages:** Settings → Pages → Source "Deploy from a branch" → branch `main`, folder `/docs`.

**Edit the content:** everything shown on the page comes from two blocks at the top of the `<script>` in `docs/index.html`:

- `SITE` – name, contact email, LinkedIn and YouTube links, and `formEndpoint` (leave empty to keep the request form in preview mode, or point it at a form service such as formsubmit.co to send email).
- `PANELS` – the ribbon panels and tools of the BIM Tools tab (the real lineup, without the seven retired tools).
- `FAMILIES`, `FILES`, `COURSES`, `NOTES`, `CHANGELOG` – sample content to replace with your own.

The interactive previews (planting scatter, shadow analysis, tag alignment, the pergola flex test, the file browser and the course syllabus) are plain JavaScript inside the same file.
