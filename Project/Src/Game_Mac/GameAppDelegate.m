// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.

#import "GameAppDelegate.h"

#include <dlfcn.h>

typedef int mono_main(int argc, char* argv[]);
typedef void mono_set_dirs(const char* assebmly_dir, const char* config_dir);

void MessageBox(NSString* text, NSString* caption)
{
	NSAlert *alert = [[NSAlert alloc] init];
	[alert setMessageText:caption];
    [alert setAlertStyle:NSCriticalAlertStyle];
    [alert setInformativeText:text];
    [alert runModal];
	[alert release];
}

NSString* GetLaunchPath()
{
	CFBundleRef mainBundle = CFBundleGetMainBundle();
	CFURLRef mainBundleURL = CFBundleCopyBundleURL(mainBundle);
	NSString* execPath = (NSString*) CFURLCopyFileSystemPath(mainBundleURL, kCFURLPOSIXPathStyle);
	NSString* execDir = [execPath stringByDeletingLastPathComponent];
	NSString* launchPath = [execDir stringByAppendingPathComponent:@"mono"];
	return launchPath;
}

bool IsDirectoryExists(NSString* path)
{
	bool result = false;

	NSFileManager* fileManager = [[NSFileManager alloc] init];
	BOOL isDir;
	if([fileManager fileExistsAtPath:path isDirectory:&isDir] && isDir)
		result = true;
	[fileManager release];

	return result;
}

NSString* GetDestinationFileName()
{
	CFBundleRef mainBundle = CFBundleGetMainBundle();
	CFURLRef mainBundleURL = CFBundleCopyBundleURL(mainBundle);
	NSString* execPathStr = (NSString*) CFURLCopyFileSystemPath(mainBundleURL, kCFURLPOSIXPathStyle);

	//check for deployed version
	NSString* destinationDirectory = [execPathStr stringByAppendingString:@"/Contents/Resources/Bin"];
	if(IsDirectoryExists(destinationDirectory))
	{
		NSString* destinationPath = [destinationDirectory stringByAppendingString:@"/Game.exe"];
		return destinationPath;
	}

	NSString* execPathLower = [execPathStr lowercaseString];
	
	NSRange foundRange = [execPathLower rangeOfString:@".app"];
	int index = foundRange.location;
	if(index == -1)
	{
		MessageBox(@"Invalid executable file name.\n\nDemands file name in format \"{destination base file name}.app\".",
			@"Error");
		return @"";
	}
	
	NSString* basePath = [execPathStr substringToIndex:index];
	NSString* destinationPath = [basePath stringByAppendingString:@".exe"];
	
	return destinationPath;
}

NSString* GetExecDir()
{
	CFBundleRef mainBundle = CFBundleGetMainBundle();
	CFURLRef mainBundleURL = CFBundleCopyBundleURL(mainBundle);
	NSString* execPath = (NSString*) CFURLCopyFileSystemPath(mainBundleURL, kCFURLPOSIXPathStyle);

	//check for deployed version
	NSString* destinationDirectory = [execPath stringByAppendingString:@"/Contents/Resources/Bin"];
	if(IsDirectoryExists(destinationDirectory))
		return destinationDirectory;

	NSString* execDir = [execPath stringByDeletingLastPathComponent];
	return execDir;
}

bool IsFileExists(NSString* path)
{
	char aPath[4096];
	strcpy(aPath, (char*)[path cStringUsingEncoding:NSUTF8StringEncoding]);

	FILE* file = fopen(aPath, "r");
	if(!file)
		return false;
	fclose(file);
	return true;
}

void RunMono()
{
	NSString* execDirPath = GetExecDir();
	NSString* monoRuntimeLocalPath = [execDirPath stringByAppendingPathComponent:@"/NativeDlls/MacOSX_x86/MonoRuntime"];

	//load mono dylib

	NSString* monoDllPath = [monoRuntimeLocalPath stringByAppendingPathComponent:@"lib/libmono-2.0.1.dylib"];

	char monoDllPathAnsi[4096];
	strcpy(monoDllPathAnsi, (char*)[monoDllPath cStringUsingEncoding:NSUTF8StringEncoding]);

	void* monoDll = dlopen( monoDllPathAnsi, RTLD_LAZY | RTLD_GLOBAL);
	if(!monoDll)
	{
		NSString* resultStr = [NSString stringWithFormat:@"Unable to load library \"%@\".", monoDllPath];
		MessageBox(resultStr, @"Error");
		return;
	}

	//get mono functions

	mono_main* monoMainFunction = (mono_main*)dlsym( monoDll, "mono_main" );
	if(!monoMainFunction)
	{
		MessageBox(@"No \"mono_main\" procedure.", @"Error");
		return;
	}

	mono_set_dirs* monoSetDirsFunction = (mono_set_dirs*)dlsym( monoDll, "mono_set_dirs" );
	if(!monoSetDirsFunction)
	{
		MessageBox(@"No \"mono_set_dirs\" procedure.", @"Error");
		return;
	}

	//set mono dirs
	{
		NSString* monoLibPath = [monoRuntimeLocalPath stringByAppendingPathComponent:@"lib"];
		NSString* monoEtcPath = [monoRuntimeLocalPath stringByAppendingPathComponent:@"etc"];

		char monoLibPathAnsi[4096];
		strcpy(monoLibPathAnsi, (char*)[monoLibPath cStringUsingEncoding:NSUTF8StringEncoding]);

		char monoEtcPathAnsi[4096];
		strcpy(monoEtcPathAnsi, (char*)[monoEtcPath cStringUsingEncoding:NSUTF8StringEncoding]);

		monoSetDirsFunction(monoLibPathAnsi, monoEtcPathAnsi);
	}
	
	NSString* destinationFileName = GetDestinationFileName();
	if(!IsFileExists(destinationFileName))
	{
		NSString* resultStr = [NSString stringWithFormat:@"Unable to open file \"%@\".", destinationFileName];
		MessageBox(resultStr, @"Error");
		return;
	}

	NSString* nativeLibrariesPath = [execDirPath stringByAppendingPathComponent:@"/NativeDLLs/MacOSX_x86"];
	NSString* monoConfigPath = [nativeLibrariesPath stringByAppendingPathComponent:@"MonoRuntime.config"];

	//good?
	chdir([nativeLibrariesPath cStringUsingEncoding:NSUTF8StringEncoding]);

	char argConfig[4096];
	char argPath[4096];
	strcpy(argConfig, (char*)[monoConfigPath cStringUsingEncoding:NSUTF8StringEncoding]);
	strcpy(argPath, (char*)[destinationFileName cStringUsingEncoding:NSUTF8StringEncoding]);

	int argc = 0;
	char* argv[256];
	{
		argv[argc] = "none";
		argc++;

		argv[argc] = "--config";
		argc++;
		argv[argc] = argConfig;
		argc++;

		argv[argc] = "--runtime=v4.0";
		argc++;

		argv[argc] = argPath;
		argc++;

		////command line
		//{
		//	NSArray* arguments = [[NSProcessInfo processInfo] arguments];
		//	int argumentCount = [arguments count];
		//	if(argumentCount > 1)
		//	{
		//		for(int i = 1; i < argumentCount; i++)
		//		{
		//			NSString* s = [arguments objectAtIndex:i]; 
		//        
		//			const char* cString = [s cStringUsingEncoding:NSUTF8StringEncoding];		        
		//			int stringLength = strlen(cString);		        
		//			char* cStringCopy = (char*)malloc(stringLength + 1);
		//			strcpy(cStringCopy, cString);
		//        
		//			argv[argc] = cStringCopy;        
		//			argc++;
		//		}
		//	}
		//}
	}

	setenv("MONO_IOMAP", "all", 1);

	int result = monoMainFunction(argc, argv);

	//[macArguments release];

	//if(result != 0)
	//{
	//	NSString* resultStr = [NSString stringWithFormat:@"Mono return %d", result];
	//	MessageBox(resultStr, @"Error");
	//}

	exit(result);
	//crash on 10.5
	//[NSApp terminate: nil];
}

@implementation GameAppDelegate

@synthesize window;

- (void)applicationWillFinishLaunching:(NSNotification *)aNotification
{
	[NSApplication sharedApplication];

	[window setBackgroundColor:[NSColor blackColor]];
}

- (void)applicationDidFinishLaunching:(NSNotification *)aNotification
{
	RunMono();
}

- (BOOL)applicationShouldTerminateAfterLastWindowClosed:(NSApplication *)theApplication
{
	return YES;
}

@end
