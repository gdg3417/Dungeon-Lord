#import <UIKit/UIKit.h>

// Unity renders in physical pixels; UIKit interaction dimensions use points.
extern "C" float DungeonLordScreenPointsScale()
{
    return (float)[UIScreen mainScreen].nativeScale;
}
