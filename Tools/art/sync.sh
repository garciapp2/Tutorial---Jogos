#!/bin/sh
# Gera a pixel art com o pxl e copia para o projeto Unity.
set -e
cd "$(dirname "$0")/../.."
node --experimental-strip-types --no-warnings Tools/pxl.mjs build Tools/art/cold_ember.px.js --no-preview
cp Tools/art/out/sprites/*.png Assets/Art/Pixel/
cp Tools/art/out/anims/*.png Tools/art/out/anims/*.json Assets/Art/Pixel/Anims/
mv -f Assets/Art/Pixel/ui_notch.png Assets/Art/Pixel/ui_pixel.png Assets/Resources/
