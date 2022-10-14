#if UNITY_EDITOR || DEVELOPMENT_BUILD

using UnityEngine;

[CreateAssetMenu(fileName = "HomoAddressableConfig", menuName = "Addressables/HomoAddressableConfig", order = 1)]
public class HomoAddressableConfig: ScriptableObject {
	public enum LogLevel {
		None, Log, Warning, Error
	}
	public LogLevel SucceededLogLevel = LogLevel.Log;
	public LogLevel CanceledLogLevel = LogLevel.Warning;
	public LogLevel TimeoutLogLevel = LogLevel.Warning;
	public LogLevel FailedLogLevel = LogLevel.Error;

	public LogLevel ReleaseSenderNullWarning = LogLevel.Warning;
}
#endif