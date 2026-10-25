#!/bin/sh
# Builds the native library for every platform from a Mac into the Unity package:
#   macOS (universal), Windows x86_64, Linux x86_64 (glibc 2.17+), iOS arm64 (static),
#   Android arm64-v8a and armeabi-v7a (API 23, 16 KB pages).
#
# Requires:
#   rustup target add aarch64-apple-darwin x86_64-apple-darwin x86_64-pc-windows-gnu \
#       x86_64-unknown-linux-gnu aarch64-apple-ios aarch64-linux-android armv7-linux-androideabi
#   brew install zig && cargo install cargo-zigbuild      (Windows and Linux linker)
#   Xcode                                                  (macOS and iOS)
#   An Android NDK: set ANDROID_NDK_HOME, or Unity's Android module provides one.
set -e
cd "$(dirname "$0")"
out=../Packages/com.jaroslavstehlik.svgimporter/Runtime/Plugins

./build-macos.sh

cargo zigbuild --release -p svgimporter-core --target x86_64-pc-windows-gnu
mkdir -p "$out/Windows/x86_64"
cp target/x86_64-pc-windows-gnu/release/svgimporter_core.dll "$out/Windows/x86_64/"

cargo zigbuild --release -p svgimporter-core --target x86_64-unknown-linux-gnu.2.17
mkdir -p "$out/Linux/x86_64"
cp target/x86_64-unknown-linux-gnu/release/libsvgimporter_core.so "$out/Linux/x86_64/"

IPHONEOS_DEPLOYMENT_TARGET=13.0 cargo build --release -p svgimporter-core --target aarch64-apple-ios
mkdir -p "$out/iOS"
cp target/aarch64-apple-ios/release/libsvgimporter_core.a "$out/iOS/"
# Debug symbols and local symbols: the app's link step needs neither.
xcrun strip -S -x "$out/iOS/libsvgimporter_core.a"

ndk=${ANDROID_NDK_HOME:-$(ls -d /Applications/Unity/Hub/Editor/*/PlaybackEngines/AndroidPlayer/NDK 2>/dev/null | tail -1)}
bin="$ndk/toolchains/llvm/prebuilt/darwin-x86_64/bin"
export CARGO_TARGET_AARCH64_LINUX_ANDROID_LINKER="$bin/aarch64-linux-android23-clang"
export CC_aarch64_linux_android="$bin/aarch64-linux-android23-clang" AR_aarch64_linux_android="$bin/llvm-ar"
export CARGO_TARGET_ARMV7_LINUX_ANDROIDEABI_LINKER="$bin/armv7a-linux-androideabi23-clang"
export CC_armv7_linux_androideabi="$bin/armv7a-linux-androideabi23-clang" AR_armv7_linux_androideabi="$bin/llvm-ar"
cargo build --release -p svgimporter-core --target aarch64-linux-android
cargo build --release -p svgimporter-core --target armv7-linux-androideabi
mkdir -p "$out/Android/arm64-v8a" "$out/Android/armeabi-v7a"
cp target/aarch64-linux-android/release/libsvgimporter_core.so "$out/Android/arm64-v8a/"
cp target/armv7-linux-androideabi/release/libsvgimporter_core.so "$out/Android/armeabi-v7a/"

ls -la "$out"/*/* "$out"/*/*/* 2>/dev/null | grep -v "\.meta$"
