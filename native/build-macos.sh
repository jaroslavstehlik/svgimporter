#!/bin/sh
# Builds the universal (arm64 + x86_64) macOS library into the Unity package.
# Requires: rustup target add aarch64-apple-darwin x86_64-apple-darwin
set -e
cd "$(dirname "$0")"
export MACOSX_DEPLOYMENT_TARGET=11.0
cargo build --release -p svgimporter-core --target aarch64-apple-darwin
cargo build --release -p svgimporter-core --target x86_64-apple-darwin
out=../Packages/com.jaroslavstehlik.svgimporter/Runtime/Plugins/macOS
mkdir -p "$out"
lipo -create \
    target/aarch64-apple-darwin/release/libsvgimporter_core.dylib \
    target/x86_64-apple-darwin/release/libsvgimporter_core.dylib \
    -output "$out/libsvgimporter_core.dylib"
lipo -info "$out/libsvgimporter_core.dylib"
