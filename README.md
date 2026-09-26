# claude-todo-demo

## Parametra website

`docs/index.html` is a single-file website for the Parametra catalogue: Revit plugins, parametric families, Revit files and courses. It has no build step; its only external dependencies are Google Fonts and three.js (r128, loaded from cdnjs) for the hero's WebGL site model. If WebGL is unavailable the hero falls back to a 2D contour animation.

**Preview locally:** open `docs/index.html` in a browser.

**Publish with GitHub Pages:** Settings → Pages → Source "Deploy from a branch" → branch `main`, folder `/docs`.

**Edit the content:** everything shown on the page comes from two blocks at the top of the `<script>` in `docs/index.html`:

- `SITE` – name, contact email, LinkedIn and YouTube links, and `formEndpoint` (leave empty to keep the request form in preview mode, or point it at a form service such as formsubmit.co to send email).
- `PANELS` – the ribbon panels and tools of the BIM Tools tab (the real lineup, without the seven retired tools).
- `FAMILIES`, `FILES`, `COURSES`, `NOTES`, `CHANGELOG` – sample content to replace with your own. Every plugin panel, family category, file and course also has a `details` text shown on its own detail page (open by clicking the item; the URL gets a `#item-…` hash you can share).

The interactive previews (planting scatter, shadow analysis, tag alignment, the pergola flex test, the file browser and the course syllabus), the 3D hero scene and the tilt cards are plain JavaScript inside the same file.

## Editing the site without code (admin mode)

The page has a built-in editor.

- **On the Claude artifact link:** the owner sees an **Admin** button at the bottom left. Click it, edit any outlined text directly on the page, and use the drawer tabs to change the site details, ribbon panels, families, files, courses, notes and changelog. **Save & publish** writes a new version of the artifact that every visitor sees. If the button is missing, add `#admin` to the end of the link.
- **On GitHub Pages (this repo's `docs/index.html`):** open the page with `#admin` at the end of the URL. Edits save in your own browser; to publish them, open the **Import / export** tab, copy the JSON, and commit it as `docs/content.json`. The page loads that file automatically for every visitor.

The **Site** tab also holds the motion options: the button hover effect (shimmer, liquid, beam or magnetic; applied only to the call-to-action buttons) and the section transition used when a menu link or a section dot hops to another part of the page (warp, fade, slide or none). Scrolling normally also animates each section in: a light sweeps across its divider, its content cascades in, and the hero recedes as you leave it. With the **3D motion** option on (the default), a particle sculpture in the page's colours morphs with the scroll (a parametric frame that breaks into a cloud, streams past the camera, regroups into a grid and settles into a ring, with bursts on fast scrolling), while panels tilt, cards fly in from the sides and text parallaxes as they pass through the viewport. The sculpture also glides to a set position, size and opacity for each section (the `POSES` table in the script), the section dots show their label for a moment when a section becomes active, and a glowing progress bar runs along the top edge.

Defaults live in the `SITE` and data blocks inside `docs/index.html`; the editor's **Reset all** returns to them.
