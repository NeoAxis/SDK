// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.

#import <Cocoa/Cocoa.h>

//for 10.5
@interface GameAppDelegate : NSObject
//@interface GameAppDelegate : NSObject<NSApplicationDelegate>
{
    NSWindow *window;
}

@property (assign) IBOutlet NSWindow *window;

- (BOOL)applicationShouldTerminateAfterLastWindowClosed:(NSApplication *)theApplication;

@end
