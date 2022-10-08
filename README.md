HomoAddressable
===
一个更人性化的 `Addressable` 资源读取解决方案。人的学名前缀便是 Homo ，本着以人为本的思想创建了这个工具，所以给它起名叫做 HomoAddressable。（大嘘）

* 自动管理 `LoadAsset` 所生成的 `AsyncOperationHandle` ，在读取资源的 `GameObject` 实例销毁时自动释放；
* 使用 `Unitask` 作为异步的方式，让读取资源后的回调更加简单；
* 使用路径直接读取 `Sprite` ，亦可通过路径以及计数读取 `Multiple Sprites` (由于本人没有用过 `Sprite Atlas`，不知是否有单独封装的必要，等日后再研究）

安装
---
### 通过 git URL 安装

你也可以通过在 Unity 引擎中，打开 `Package Manager` ，点击左上角的 `+` ，选择 `Add package from git URL` ，然后输入本项目的 git 地址即可： 
`https://github.com/Giresharu/HomoAddressable.git?path=Assets` 。

依赖
---
本工具依赖于 [com.unity.addressable](https://docs.unity3d.com/Packages/com.unity.addressables@1.19/manual/AddressableAssetsGettingStarted.html) 与 [com.cysharp.unitask](https://github.com/Cysharp/UniTask) 运行，请确保项目以安装这两个插件。

HomoBehaviour 类与 IOnDestroyAssetLoad 接口
---

### HomoBehaviour

通常我们会在一个 `MonoBehaviour` 类中读取并使用资源。所以我们可以在 `MonoBehaviour` 类销毁时，释放它所加载的所有资源的 `AsyncOperationHandle` 。而 `HomoBehaviour` 类正是实现了这个功能的派生类。

由于我们在 `HomoBehaviour` 中对 `OnDestroy` 进行了实现，若派生类也需要实现 `OnDestroy` 时，请在 `OnDestroy` 时调用 `base.OnDestroy` ：
```cs
new void OnDestroy() {
	base.OnDestroy();
}
```

### IOnDestroyAssetLoad

我建议所有需要读取资源的 `MonoBehaviour` 类都改成使用 `HomoBehaviour` ，但如果你的项目中已经有着许多继承于 `MonoBehaviour` 且不方便修改的类，你可以选择手动继承 `IOnDestroyAssetLoad` 接口并实现：
```cs
using System.Collections.Generic;
using HomoAddressableTools;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Test : MonoBehaviour, IOnDestroyAssetLoad {

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
```

### 关于非 MonoBehaviour 类

非 `MonoBehaviour` 类不存在 `OnDestroy` ；而 `Addressable.Release` 函数在析构函数中不产生作用。对此我的建议是：
* 如果非 `MonoBehaviour` 类是一个需要读取资源的数据结构，请让使用此数据结构的 `MonoBehaviour` 类来承当绑定资源与释放的作用；
* 如果该类是一个需要自己读取资源的静态类，则建议使用原生 `Addressable` 来进行手动加载与释放。

HomoAddressable.LoadAssetAsync<T>
---
LoadAssetAsync<T> 的签名有两种：
```cs
UniTask<T> LoadAssetAsync<T>(this IOnDestroyAssetLoad bindObj, string path, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken token = default)

UniTask<T> LoadAssetAsync<T>(this IOnDestroyAssetLoad bindObj, AssetReference reference, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken token = default)
```
* 其中 `bindObj` 为资源所绑定的实现了 `IOnDestroyAssetLoad` 接口的 `MonoBehaviour` 实例，当我们销毁这个实例时，这次加载的资源也会自动被释放；
* `path` 即为资源在 `Addressable` 中的路径，通常来说，只要你不去修改，它默认就是资源在项目中的路径，可以方便地搭配诸如 `Odin` 等插件进行方便地选择；
* 也可以不用路径而是用重载的 `AssetReference` 来读取资源；
* `progress` 是一个返回资源读取进度的委托；
* `timing` 表示异步操作在主线程循环的哪一步执行，具体可以参考 UniTask 的文档；
* `token` 则是取消信号，当我们不填或者填写 `default` 时，它会被视为 `bindObj` 的 `GetCancellationTokenOnDestroy` ，如果在读取完之前，所绑定的 `MonoBehaviour` 就被销毁，则任务会被取消。

```cs
TextAsset data;
float data_progress;
	
async void Load() {
	data = await HomoAddressable.LoadAssetAsync<TextAsset>(this, "Asset/Data/data.csv",new Progress<float>(progress =>data_progress = progress));
}
```
使用扩展方法的形式：
```cs
data = await this.LoadAssetAsync<TextAsset>("Asset/Data/data.csv",new Progress<float>(progress =>data_progress = progress));
```

HomoAddressable.LoadAsset<T>
---
同步版本的 `LoadAssetAsync<T>`，由于是同步的，它不像异步版本拥有那么多参数：
```cs
T LoadAsset<T>(this IOnDestroyAssetLoad bindObj, string path)
T LoadAsset<T>(this IOnDestroyAssetLoad bindObj, AssetReference reference)
```

读取 Sprite
---
由于在 Unity 中，读取 `Sprite` 无法直接通过路径，而是必须在后面在接上一个 `[文件名_编号]` 或着 `[你重命名过后的名字]` 。就算图片是单张的，也仍然要使用 `[文件名]` 作为后缀，才能读取到资源。所以我封装了几个更便捷的获取方法：

### LoadSpriteAsync

```cs
UniTask<Sprite> LoadSpriteAsync(this IOnDestroyAssetLoad bindObj, string path, int index = -1, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken token = default)

UniTask<Sprite> LoadSpriteAsync(this IOnDestroyAssetLoad bindObj, string path, string spriteName, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken token = default)
```

* 大部分都和 `LoadAssetAsync<T>` 的参数是一样的，这里就不再多做赘述；
* `index` 指的是读取的 `Sprite` 在切割后的索引，也就是本身后缀名中数字的部分。如果留空不填为 -1 时，则表示直接用 `[文件名]` 作为后缀来读取单张的 `Sprite`；
* 如果对切割后的 `Sprite` 进行了重命名，则需要使用第二个重载：参数 `spriteName` 就是你重命名的名字；
* 由于使用 `AssetReference` 是，本身就可以方便地选择到 `Sprite` ，直接使用 `LoadAssetAsync<Sprite>` 即可。所以并未实现 `LoadSpriteAsync` 的 `AssetReference` 方式的重载。

LoadSpriteAsync 也有同步版本 LoadSprite ：

```cs
Sprite LoadSprite(this IOnDestroyAssetLoad bindObj, string path, int index = -1)

Sprite LoadSprite(this IOnDestroyAssetLoad bindObj, string path, string spriteName)
```

### LoadSpritesAsync

若要一口气读取同一个图片文件中切割的多个 `Sprite` ，则可以使用 `LoadSpritesAsync` ：

```cs
UniTask<Sprite[]> LoadSpritesAsync(this IOnDestroyAssetLoad bindObj, string path, int count = 1, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken token = default)
```

* 由于我没有找到获取图片文件包含多少 `Sprite` ，以及它们各自的名字的 `Api` ，这个函数只能运用在没有被重命名的 `Sprite` 上；
* `count` 参数表示读取的范围数量，比如要读取全部 12 张 `Sprite` ，则填写 12 ，函数会返回索引为 0~11 的` Sprite` 数组；
* 由于本人的智商原因， `progress` 暂时只能粗略地获取已经读取的 `Sprite` 占比多少，无法获得正在读取的 `Sprite` 的进度。

同步版本：

```cs
LoadSprites(this IOnDestroyAssetLoad bindObj, string path, int count = 1)
```

待补充
---





