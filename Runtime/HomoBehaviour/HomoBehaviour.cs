using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace HomoAddressableTools {
	public abstract class HomoBehaviour: MonoBehaviour, IOnDestroyAssetLoad {
		public event OnDestroyDelegate OnDestroyEvent;
		public List<string> Keys { get; set; }
		public Dictionary<string, AsyncOperationHandle<Object>> Handles { get; set; }

		protected void OnDestroy() {
			OnDestroyInvoke();
		}

		void OnDestroyInvoke() {
			OnDestroyEvent?.Invoke(this);
		}
	}
}