#!/bin/sh
rm -rf ../../Bin/Game.app 
mkdir ../../Bin/Game.app
mkdir ../../Bin/Game.app/Contents
cp -r /Temp/_Compilation/MacOS/Game.app/Contents/* ../../Bin/Game.app/Contents/
mkdir ../../Bin/Game.app/Contents/Frameworks
cp -r AdditionalFiles/Frameworks/* ../../Bin/Game.app/Contents/Frameworks/
