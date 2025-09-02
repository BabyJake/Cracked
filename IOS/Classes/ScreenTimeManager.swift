import Foundation
import FamilyControls
import DeviceActivity

@objc public class ScreenTimeManager: NSObject, @unchecked Sendable {
    @objc public static let shared = ScreenTimeManager()
    
    @objc public func requestAuthorization() {
        Task {
            do {
                try await AuthorizationCenter.shared.requestAuthorization(for: .individual)
                print("✅ Screen Time authorization granted")
            } catch {
                print("❌ Failed to get authorization: \(error)")
            }
        }
    }
}
