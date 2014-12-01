# DO NOT EDIT
# This makefile makes sure all linkable targets are
# up-to-date with anything they link to, avoiding a bug in XCode 1.5
all.Debug: \
	/Users/apple/Desktop/openal-soft-1.13/out/Debug/libopenal.dylib\
	/Users/apple/Desktop/openal-soft-1.13/out/Debug/openal-info

all.Release: \
	/Users/apple/Desktop/openal-soft-1.13/out/Release/libopenal.dylib\
	/Users/apple/Desktop/openal-soft-1.13/out/Release/openal-info

all.MinSizeRel: \
	/Users/apple/Desktop/openal-soft-1.13/out/MinSizeRel/libopenal.dylib\
	/Users/apple/Desktop/openal-soft-1.13/out/MinSizeRel/openal-info

all.RelWithDebInfo: \
	/Users/apple/Desktop/openal-soft-1.13/out/RelWithDebInfo/libopenal.dylib\
	/Users/apple/Desktop/openal-soft-1.13/out/RelWithDebInfo/openal-info

# For each target create a dummy rule so the target does not have to exist
/Users/apple/Desktop/openal-soft-1.13/out/Debug/libopenal.dylib:
/Users/apple/Desktop/openal-soft-1.13/out/MinSizeRel/libopenal.dylib:
/Users/apple/Desktop/openal-soft-1.13/out/RelWithDebInfo/libopenal.dylib:
/Users/apple/Desktop/openal-soft-1.13/out/Release/libopenal.dylib:


# Rules to remove targets that are older than anything to which they
# link.  This forces Xcode to relink the targets from scratch.  It
# does not seem to check these dependencies itself.
/Users/apple/Desktop/openal-soft-1.13/out/Debug/libopenal.dylib:
	/bin/rm -f /Users/apple/Desktop/openal-soft-1.13/out/Debug/libopenal.dylib


/Users/apple/Desktop/openal-soft-1.13/out/Debug/openal-info:\
	/Users/apple/Desktop/openal-soft-1.13/out/Debug/libopenal.dylib
	/bin/rm -f /Users/apple/Desktop/openal-soft-1.13/out/Debug/openal-info


/Users/apple/Desktop/openal-soft-1.13/out/Release/libopenal.dylib:
	/bin/rm -f /Users/apple/Desktop/openal-soft-1.13/out/Release/libopenal.dylib


/Users/apple/Desktop/openal-soft-1.13/out/Release/openal-info:\
	/Users/apple/Desktop/openal-soft-1.13/out/Release/libopenal.dylib
	/bin/rm -f /Users/apple/Desktop/openal-soft-1.13/out/Release/openal-info


/Users/apple/Desktop/openal-soft-1.13/out/MinSizeRel/libopenal.dylib:
	/bin/rm -f /Users/apple/Desktop/openal-soft-1.13/out/MinSizeRel/libopenal.dylib


/Users/apple/Desktop/openal-soft-1.13/out/MinSizeRel/openal-info:\
	/Users/apple/Desktop/openal-soft-1.13/out/MinSizeRel/libopenal.dylib
	/bin/rm -f /Users/apple/Desktop/openal-soft-1.13/out/MinSizeRel/openal-info


/Users/apple/Desktop/openal-soft-1.13/out/RelWithDebInfo/libopenal.dylib:
	/bin/rm -f /Users/apple/Desktop/openal-soft-1.13/out/RelWithDebInfo/libopenal.dylib


/Users/apple/Desktop/openal-soft-1.13/out/RelWithDebInfo/openal-info:\
	/Users/apple/Desktop/openal-soft-1.13/out/RelWithDebInfo/libopenal.dylib
	/bin/rm -f /Users/apple/Desktop/openal-soft-1.13/out/RelWithDebInfo/openal-info


