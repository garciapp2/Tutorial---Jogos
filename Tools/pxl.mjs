// Roda o pxl (github.com/crsolver/claude-pxl) clonado em Tools/claude-pxl.
// O bin/pxl.js original exige Node >= 22.18; este atalho funciona a partir do 22.6:
//   node --experimental-strip-types --no-warnings Tools/pxl.mjs build Tools/art/cold_ember.px.js
import { pathToFileURL, fileURLToPath } from "node:url";
import { join, dirname } from "node:path";
const here = dirname(fileURLToPath(import.meta.url));
const { main } = await import(pathToFileURL(join(here, "claude-pxl/src/node/cli.ts")).href);
process.exitCode = await main(process.argv.slice(2));
