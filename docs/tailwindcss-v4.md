Here’s a concise summary **followed by** a detailed breakdown of the major changes in Tailwind CSS **v4** that AI tools trained primarily on v3 must be aware of.

---

### ✅ Summary

Tailwind CSS v4 is a substantial update. The major shifts include:

* A new **engine** with much faster build times. ([tailwindcss.com][1])
* A move from JS-based configuration (`tailwind.config.js`) to a **CSS-first configuration** using `@theme`, native CSS variables, cascade layers. ([tailwindcss.com][1])
* Dropping older browser support and leaning on modern CSS features (container queries, `color-mix()`, etc). ([tailwindcss.com][2])
* New utilities and capabilities (3D transforms, expanded gradients, container queries, text-shadows) and changed defaults. ([tailwindcss.com][1])
* Configuration/tooling changes: e.g., new PostCSS plugin package, new CLI plugin, simplified installation. ([tailwindcss.com][2])

For an AI that “knows” Tailwind v3, these shifts mean its assumptions about configuration structure, class-defaults, browser support, build workflow, and some utilities may be outdated.

---

### 🧠 Detailed Breakdown for AI/integration purposes

Here are key change-areas an AI should flag when working with v4 vs v3:

#### 1. Engine & Build Performance

* v4 has a “ground-up rewrite” of the engine: full builds up to ~3.8× faster, incremental builds up to ~100× faster (measured in micro-seconds) compared to v3.4. ([tailwindcss.com][1])
* Because of this rewrite, internal assumptions about build step tooling, caching behavior, and output control may differ (e.g., fewer “no changes => still rebuild” scenarios).
* For AI-automated code generation: assuming “slow builds” or “heavy config” is outdated for v4.

#### 2. Configuration: CSS-First vs JS-Config

* In v3, configuration is typically via `tailwind.config.js` (or .ts). In v4, the recommended path is configuration **in CSS** via `@theme { … }`, `@utility`, using native CSS variables and cascade layers. ([tailwindcss.com][1])
* The import directive changes: instead of separate `@tailwind base; @tailwind components; @tailwind utilities;`, you now typically have `@import "tailwindcss";` and rely on automatic layering. ([DEV Community][3])
* The CSS variables (“design tokens”) are exposed by default (colours, fonts, breakpoints) enabling dynamic theming. ([MojoAuth][4])
* For AI toolchains: any logic generating or parsing `tailwind.config.js` must consider that v4 may not require or use it; configuration could reside in CSS.

#### 3. Browser & Feature Support

* Tailwind v4 expects “modern web”: Safari 16.4+, Chrome 111+, Firefox 128+. Support for older browsers is no longer guaranteed. ([tailwindcss.com][2])
* Because the framework leverages newer CSS features like `@property`, `color-mix()`, container queries, cascade layers, native nesting – an AI must not rely on polyfills for older browsers unless explicitly configured. ([tailwindcss.com][1])
* If your UI stack (desktop tooling or hybrid) has to support older browsers/environments (or embedded web views), that’s a compatibility risk.

#### 4. New / Changed Utilities & Defaults

* Expanded gradient APIs: radial, conic, interpolation modes. ([tailwindcss.com][1])
* 3D transform utilities added: e.g., `rotate-x`, `rotate-y`, `scale-z`, `translate-z`. ([Medium][5])
* Container queries are now first-class: no plugin needed. ([tailwindcss.com][1])
* Text-shadow utilities introduced in v4.1. ([tailwindcss.com][6])
* New defaults: a modernised P3 colour palette (for wide gamut displays). ([tailwindcss.com][1])
* Some utility classes, scales and naming may have changed or been deprecated (though the official guide emphasises migration tools handle much of this). ([DEV Community][3])

#### 5. Tooling & Installation Changes

* The PostCSS plugin for Tailwind moved: in v3 you used `tailwindcss` as a PostCSS plugin; in v4 there's a dedicated `@tailwindcss/postcss` plugin. ([tailwindcss.com][2])
* For Vite users: a dedicated `@tailwindcss/vite` plugin exists and is recommended for best experience. ([tailwindcss.com][2])
* CLI: the `tailwindcss` CLI lives in `@tailwindcss/cli` package in v4. ([tailwindcss.com][2])
* Automated upgrade tool: you can run `npx @tailwindcss/upgrade` (requires Node.js 20+). ([tailwindcss.com][2])

---

### 🔍 AI-specific considerations (for your automation / code generation context)

Since you “teach” or integrate AI (or use AI tools) that know Tailwind v3, here are specific points you’ll want to adjust:

* **Config File Handling**: AI should default to anticipating a `tailwind.config.js` in v3, but for v4 it may either not exist or be minimal; the configuration is in CSS. AI prompts/scripts must allow for that alternative.
* **Class Naming / Utility Availability**: If AI suggests classes (e.g., 3D transforms, container queries), verify they exist in v4. Conversely, AI should avoid using deprecated classes without warning.
* **Build Toolchain Assumptions**: AI should not assume autoprefixer/config manually added; many of those concerns are now internalised.
* **Browser Compatibility Assumptions**: When generating UI code (for your internal tooling), AI should validate whether target browsers for your environment match the “modern web” target of v4 or if you need compatibility fallbacks (or remain on v3).
* **Theming / Design Token Handling**: When generating code for themes, AI should leverage CSS variables (exposed by Tailwind v4) rather than only JS configuration. That enables runtime theming.
* **Migration Consideration**: If AI is used to migrate code from v3 to v4, it should identify the changed defaults/utilities, locate any custom config sections in `tailwind.config.js` and convert them into `@theme` blocks in CSS, adjusting imports accordingly.
* **Generate Prompt Templates Accordingly**: If you have an LLM-powered generation pipeline that assumes “Tailwind v3 style”, you’ll need to parameterize the version so the model uses the correct configuration style, utilities and feature set.
