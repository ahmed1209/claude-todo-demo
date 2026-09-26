# claude-todo-demo

Two things live in this repository:

- **Revit Model Slimmer**, a Revit add-in (Revit 2023 to 2026) that shrinks and speeds up project files. Details just below and in [RevitModelSlimmer/README.md](RevitModelSlimmer/README.md).
- **The Parametra website**, the public site for the studio's Revit plugins, parametric families, Revit files and courses, published from `docs/`.

## Revit Model Slimmer

**Revit Model Slimmer** is a Revit add-in (Revit 2023 to 2026) that shrinks and speeds up project files by cleaning unused families, filters, empty views, view templates and worksets, auditing performance hogs and compacting the file.

**To install:** download [RevitModelSlimmer/RevitModelSlimmer-Installer.zip](RevitModelSlimmer/RevitModelSlimmer-Installer.zip), extract it, and double-click `Install.bat`. Then start Revit and look for the **Model Slimmer** ribbon tab.

See [RevitModelSlimmer/README.md](RevitModelSlimmer/README.md) for the full feature list, build instructions and safety notes.

## Parametra website

`docs/index.html` is a single-file website for the Parametra catalogue: Revit plugins, parametric families, Revit files and courses. It has no build step; its only external dependencies are Google Fonts and three.js (r128, loaded from cdnjs) for the hero's WebGL site model. If WebGL is unavailable the hero falls back to a 2D contour animation.

**Preview locally:** open `docs/index.html` in a browser.

**Publish with GitHub Pages:** automatic; every push to `main` that touches `docs/` runs `.github/workflows/pages.yml`. See **Going live on your own domain** below for the full route to an official address.

**Edit the content:** everything shown on the page comes from two blocks at the top of the `<script>` in `docs/index.html`:

- `SITE` – name, contact email, LinkedIn and YouTube links, and `formEndpoint` (leave empty to keep the request form in preview mode, or point it at a form service such as formsubmit.co to send email).
- `PANELS` – the ribbon panels and tools of the BIM Tools tab (the real lineup, without the seven retired tools).
- `FAMILIES`, `FILES`, `COURSES`, `NOTES`, `CHANGELOG` – sample content to replace with your own. Every plugin panel, family category, file and course also has a `details` text shown on its own detail page (open by clicking the item; the URL gets a `#item-…` hash you can share).

The interactive previews (planting scatter, shadow analysis, tag alignment, the pergola flex test, the file browser and the course syllabus), the 3D hero scene and the tilt cards are plain JavaScript inside the same file.

## Editing the site without code (admin mode)

The page has a built-in editor.

- **On the Claude artifact link:** the owner sees an **Admin** button at the bottom left. Click it, edit any outlined text directly on the page, and use the drawer tabs to change the site details, ribbon panels, families, files, courses, notes and changelog. **Save & publish** writes a new version of the artifact that every visitor sees. If the button is missing, add `#admin` to the end of the link.
- **On GitHub Pages (this repo's `docs/index.html`):** open the page with `#admin` at the end of the URL. Edits save in your own browser; to publish them, open the **Import / export** tab, copy the JSON, and commit it as `docs/content.json`. The page loads that file automatically for every visitor.

The **Site** tab also holds the motion options: the button hover effect (shimmer, liquid, beam or magnetic; applied only to the call-to-action buttons) and the section transition used when a menu link or a section dot hops to another part of the page (warp, fade, slide or none). Scrolling normally also animates each section in: a light sweeps across its divider, its content cascades in, and the hero recedes as you leave it. With the **3D motion** option on (the default), a particle sculpture in the page's colours morphs with the scroll (a parametric frame that breaks into a cloud, streams past the camera, regroups into a grid and settles into a ring, with bursts on fast scrolling), while panels tilt, cards fly in from the sides and text parallaxes as they pass through the viewport. The sculpture also glides to a set position, size and opacity for each section (the `POSES` table in the script), the section dots show their label for a moment when a section becomes active, and a glowing progress bar runs along the top edge. Between sections the page zooms in 3D: as you scroll across a boundary, the section you are leaving shrinks and fades into the distance towards the centre of the screen while the next one grows in from far away until it fills the viewport at full size (the `zoom3d` block in the script; it follows the same **3D motion** switch and is skipped when the visitor prefers reduced motion).

Defaults live in the `SITE` and data blocks inside `docs/index.html`; the editor's **Reset all** returns to them.

## Going live on your own domain

Everything under `docs/` is a complete public build: the page carries a search title and description, Open Graph and Twitter card tags with `og.png` (1200 × 630), an SVG favicon plus PNG and Apple touch icons, `site.webmanifest` (installable on phones), `robots.txt`, `sitemap.xml`, a styled `404.html` that sends visitors home, `.nojekyll` and a `<noscript>` notice. Three steps remain, all in your own accounts.

**1. Register the domain.** `parametra.studio` was unregistered when checked on 26 September 2026, and it matches the contact address already on the site. `parametra.tools`, `parametra.dev`, `parametra.build`, `parametra.pro` and `getparametra.com` were free too; `.com`, `.io`, `.app`, `.net` and `.design` are taken. Any registrar works (Cloudflare Registrar, Porkbun and Namecheap are common choices); `.studio` typically costs in the range of USD 20 to 35 a year, and WHOIS privacy should be included or added.

**2. GitHub Pages deploys by itself.** The workflow in `.github/workflows/pages.yml` publishes `docs/` to GitHub Pages on every push to `main` (it can also be started by hand from the Actions tab). Its first run switches Pages on for the repository; if that run stops with a permissions message instead, open Settings → Pages once, set Source to "GitHub Actions" and re-run the workflow. The site is then live at https://ahmed1209.github.io/claude-todo-demo/ .

**3. Point the domain at GitHub.** In the registrar's DNS panel add these records (the values are GitHub Pages' published addresses):

| Type | Name | Value |
|---|---|---|
| A | @ | 185.199.108.153 |
| A | @ | 185.199.109.153 |
| A | @ | 185.199.110.153 |
| A | @ | 185.199.111.153 |
| AAAA | @ | 2606:50c0:8000::153 |
| AAAA | @ | 2606:50c0:8001::153 |
| AAAA | @ | 2606:50c0:8002::153 |
| AAAA | @ | 2606:50c0:8003::153 |
| CNAME | www | ahmed1209.github.io |

Back in Settings → Pages enter the domain (for example `parametra.studio`) under "Custom domain", wait for the DNS check to pass (usually minutes, at most a day), then tick "Enforce HTTPS"; GitHub issues the certificate. `www.parametra.studio` redirects to the bare domain automatically. Finally bind the build to the domain and push:

```bash
python3 scripts/bind-domain.py parametra.studio
git add docs && git commit -m "Bind the site to parametra.studio" && git push
```

The script rewrites the canonical, social-preview, structured-data and sitemap URLs to the new domain and writes `docs/CNAME`, which is the file Pages reads to serve the domain (GitHub writes the same file when you save the custom domain, so the two agree). Recommended afterwards: in your personal GitHub Settings → Pages → "Add a domain", verify the domain so nobody else can attach it to their Pages site.

**Email.** The site shows hello@parametra.studio as the contact address, so that mailbox has to exist once the domain does. Cloudflare Email Routing (free) or the registrar's email forwarding can pass it on to your everyday inbox; or change the address in the admin editor's Site tab.
