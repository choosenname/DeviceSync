https://github.com/dotnet/maui/pull/5562
Description of Change

This PR is based on part of a discussion where it sort of feels the Essentials API doesn't really fit with Maui right now. It is just a blob of stuff that things use, but in fact it is a large part of what makes Maui a real framework.

Xamarin.Forms and now .NET MAUI is cool and has buttons and text boxes, but that is just a part of making apps. Apps require things like sharing, file picking, sensors and more. We created Xamarin.Essentials as a nice small package that had all the tools you would need.

This PR is to basically morph the namespaces so that it makes separation of the types and potentially splitting the dll into smaller parts. Not sure if that is a good or bad things right now, but this makes the namespaces fit more with an app framework.

    NOTE: The #5407 PR got so huge with the API fixes and the namespace changes. This PR is just the namespace changes, no code changes.

New API

namespace Microsoft.Maui.Accessibility {
    public interface ISemanticScreenReader {}
    public static class SemanticScreenReader {}
}
namespace Microsoft.Maui.ApplicationModel {
    public enum ActivityState {}
    public class ActivityStateChangedEventArgs : EventArgs {}
    public class AppAction {}
    public class AppActionEventArgs : EventArgs {}
    public static class AppActions {}
    public static class AppInfo {}
    public enum AppPackagingModel {}
    public enum AppTheme {}
    public static class Browser {}
    public enum BrowserLaunchFlags {}
    public enum BrowserLaunchMode {}
    public class BrowserLaunchOptions {}
    public enum BrowserTitleMode {}
    public class FeatureNotEnabledException : InvalidOperationException {}
    public class FeatureNotSupportedException : NotSupportedException {}
    public interface IAppActions {}
    public interface IAppInfo {}
    public interface IBrowser {}
    public interface ILauncher {}
    public interface IMap {}
    public interface IVersionTracking {}
    public static class Launcher {}
    public enum LayoutDirection {}
    public static class MainThread {}
    public static class Map {}
    public class MapLaunchOptions {}
    public enum NavigationMode {}
    public class OpenFileRequest {}
    public class PermissionException : UnauthorizedAccessException {}
    public static class Permissions {}
    public enum PermissionStatus {}
    public static class Platform {}
    public static class VersionTracking {}
}
namespace Microsoft.Maui.ApplicationModel.Communication {
    public class Contact {}
    public class ContactEmail {}
    public class ContactPhone {}
    public static class Contacts {}
    public static class Email {}
    public class EmailAttachment : FileBase {}
    public enum EmailBodyFormat {}
    public class EmailMessage {}
    public interface IContacts {}
    public interface IEmail {}
    public interface IPhoneDialer {}
    public interface ISms {}
    public static class PhoneDialer {}
    public static class Sms {}
    public class SmsMessage {}
}
namespace Microsoft.Maui.ApplicationModel.DataTransfer {
    public static class Clipboard {}
    public interface IClipboard {}
    public interface IShare {}
    public static class Share {}
    public class ShareFile : FileBase {}
    public class ShareFileRequest : ShareRequestBase {}
    public class ShareMultipleFilesRequest : ShareRequestBase {}
    public abstract class ShareRequestBase {}
    public class ShareTextRequest : ShareRequestBase {}
}
namespace Microsoft.Maui.Authentication {
    public static class AppleSignInAuthenticator {}
    public interface IAppleSignInAuthenticator {}
    public interface IPlatformWebAuthenticatorCallback {}
    public interface IWebAuthenticator {}
    public static class WebAuthenticator {}
    public abstract class WebAuthenticatorCallbackActivity : Activity {}
    public class WebAuthenticatorOptions {}
    public class WebAuthenticatorResult {}
}
namespace Microsoft.Maui.Devices {
    public static class Battery {}
    public class BatteryInfoChangedEventArgs : EventArgs {}
    public enum BatteryPowerSource {}
    public enum BatteryState {}
    public static class DeviceDisplay {}
    public readonly struct DeviceIdiom : IEquatable<DeviceIdiom> {}
    public static class DeviceInfo {}
    public readonly struct DevicePlatform : IEquatable<DevicePlatform> {}
    public enum DeviceType {}
    public readonly struct DisplayInfo : IEquatable<DisplayInfo> {}
    public class DisplayInfoChangedEventArgs : EventArgs {}
    public enum DisplayOrientation {}
    public enum DisplayRotation {}
    public enum EnergySaverStatus {}
    public class EnergySaverStatusChangedEventArgs : EventArgs {}
    public static class Flashlight {}
    public static class HapticFeedback {}
    public enum HapticFeedbackType {}
    public interface IBattery {}
    public interface IDeviceDisplay {}
    public interface IDeviceInfo {}
    public interface IFlashlight {}
    public interface IHapticFeedback {}
    public interface IVibration {}
    public static class Vibration {}
}
namespace Microsoft.Maui.Devices.Sensors {
    public static class Accelerometer {}
    public class AccelerometerChangedEventArgs : EventArgs {}
    public readonly struct AccelerometerData : IEquatable<AccelerometerData> {}
    public enum AltitudeReferenceSystem {}
    public static class Barometer {}
    public class BarometerChangedEventArgs : EventArgs {}
    public readonly struct BarometerData : IEquatable<BarometerData> {}
    public static class Compass {}
    public class CompassChangedEventArgs : EventArgs {}
    public readonly struct CompassData : IEquatable<CompassData> {}
    public enum DistanceUnits {}
    public static class Geocoding {}
    public static class Geolocation {}
    public enum GeolocationAccuracy {}
    public class GeolocationRequest {}
    public static class Gyroscope {}
    public class GyroscopeChangedEventArgs : EventArgs {}
    public readonly struct GyroscopeData : IEquatable<GyroscopeData> {}
    public interface IAccelerometer {}
    public interface IBarometer {}
    public interface ICompass {}
    public interface IGeocoding {}
    public interface IGeolocation {}
    public interface IGyroscope {}
    public interface IMagnetometer {}
    public interface IOrientationSensor {}
    public interface IPlatformCompass {}
    public class Location {}
    public static class LocationExtensions {}
    public static class Magnetometer {}
    public class MagnetometerChangedEventArgs : EventArgs {}
    public readonly struct MagnetometerData : IEquatable<MagnetometerData> {}
    public static class OrientationSensor {}
    public class OrientationSensorChangedEventArgs : EventArgs {}
    public readonly struct OrientationSensorData : IEquatable<OrientationSensorData> {}
    public class Placemark {}
    public static class PlacemarkExtensions {}
    public enum SensorSpeed {}
}
namespace Microsoft.Maui.Media {
    public interface IMediaPicker {}
    public interface IScreenshot {}
    public interface IScreenshotResult {}
    public interface ITextToSpeech {}
    public class Locale {}
    public static class MediaPicker {}
    public class MediaPickerOptions {}
    public static class Screenshot {}
    public static class ScreenshotExtensions {}
    public enum ScreenshotFormat {}
    public class SpeechOptions {}
    public static class TextToSpeech {}
    public static class UnitConverters {}
}
namespace Microsoft.Maui.Networking {
    public enum ConnectionProfile {}
    public static class Connectivity {}
    public class ConnectivityChangedEventArgs : EventArgs {}
    public interface IConnectivity {}
    public enum NetworkAccess {}
}
namespace Microsoft.Maui.Storage {
    public abstract class FileBase {}
    public static class FilePicker {}
    public class FilePickerFileType {}
    public class FileProvider : FileProvider {}
    public enum FileProviderLocation {}
    public class FileResult : FileBase {}
    public static class FileSystem {}
    public interface IFileSystem {}
    public interface IPlatformFileSystem {}
    public interface IPlatformSecureStorage {}
    public interface IPreferences {}
    public interface ISecureStorage {}
    public class PickOptions {}
    public static class Preferences {}
    public class ReadOnlyFile : FileBase {}
    public static class SecureStorage {}
}