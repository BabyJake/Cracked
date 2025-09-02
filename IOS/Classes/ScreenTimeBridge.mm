#import <Foundation/Foundation.h>
#import "UnityInterface.h" // already in Unity project

// Manual interface declaration for ScreenTimeManager
@interface ScreenTimeManager : NSObject
+ (instancetype)shared;
- (void)requestAuthorization;
@end

extern "C" {
    void requestAuthorization() {
        [[ScreenTimeManager shared] requestAuthorization];
    }
}
