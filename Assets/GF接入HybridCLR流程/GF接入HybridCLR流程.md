# GF接入HybridCLR流程

1.PackageManager导入插件 https://github.com/HeNuoLibrary/hybridclr_unity.git

![插件导入.png](cd5fcd1e6f7af047add5ceb1e6244b6b.png)

2.自定义HybridCLR全局设置

![全局设置.png](a4f44ca9147bc057c98e5e52f1ec7713.png)

3.加载热更脚本Assembly-CSharp.dll

![加载热更脚本.png](6b392300d68cf16bfeaa66038093be11.png)

4.在HotFixDll文件夹中创建AOT列表

<img src="5394ee318efa712133aa7508b1b41691.png" alt="微信截图_20221013104515.png" style="zoom:120%;" />

![微信截图_20221013104539.png](8928299162b658ed0225fbdb341167f5.png)

5.加载AOT ,为aot assembly加载原始metadata

教程案例中的加载方式(案例中aot.dll直接放在bundle包中 获取便可知道有多少)

![加载AOT.png](8309e06cc26a6a88fab0838fbb67f9c5.png)

GF加载AOT方式(需要用这个列表直接加载所有的AOT)

![GF加载AOT的方式.png](d7fac837a2d27e9944da6c0c054766d6.png)

6.创建CompileHotfixDll 快捷键

![CompileHotfixDll.png](2006a5250aa871f9310d69acc6503b11.png)

## 热更新模块拆分 配置 HybridCLR

<img src="b0e38907eab37c0c8eb304e085db57b3.png" alt="微信截图_20221013112418.png" style="zoom:120%;" />

将AOT部分拆分为一个或多个程序集，Assembly-CSharp作为热更新程序集，同时还有其他0-N个热更新程序集。

<img src="11caed3434cbc6b019cd709e78fa8ddf.png" alt="微信截图_20221013113814.png" style="zoom:120%;" />

## 运行流程

![c121211df7f165c2c0a4a0fba39c4bf78420db5be4edae060040f8588a4f4704.png](5c8685c25cf73c31737e3b19faa5c062.png)

![热更逻辑.png](eccc2871b4789f886e3facff5e0994c0.png)

## Build打包

安装和打包

<img src="f2f70655bd4b891521c1d3c5b7ab6de7.png" alt="微信截图_20221013115458.png" style="zoom:120%;" />

<img src="22c0a245624bf71434fd81c52ec3a687.png" alt="微信截图_20221013114258.png" style="zoom:120%;" />