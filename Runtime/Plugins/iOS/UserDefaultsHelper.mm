#import <Foundation/Foundation.h>

extern "C" {
    const char* _GetUserDefault(const char* key, const char* defaultValue) {
        if (key == NULL || defaultValue == NULL) {
            return defaultValue;
        }
        
        NSString* nsKey = [NSString stringWithUTF8String:key];
        NSString* nsDefaultValue = [NSString stringWithUTF8String:defaultValue];
        NSUserDefaults* defaults = [NSUserDefaults standardUserDefaults];
        NSString* value = [defaults stringForKey:nsKey];
        
        if (value == nil) {
            value = nsDefaultValue;
        }
        
        // Allocate memory for the return string
        const char* utf8String = [value UTF8String];
        if (utf8String == NULL) {
            return defaultValue;
        }
        
        char* result = (char*)malloc(strlen(utf8String) + 1);
        strcpy(result, utf8String);
        return result;
    }
    
    void _RemoveUserDefault(const char* key) {
        if (key == NULL) {
            return;
        }
        
        NSString* nsKey = [NSString stringWithUTF8String:key];
        NSUserDefaults* defaults = [NSUserDefaults standardUserDefaults];
        [defaults removeObjectForKey:nsKey];
        [defaults synchronize];
    }
}
