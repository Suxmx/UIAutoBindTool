

功能是在搭建UI时，只要按照命名规范命名，就能自动生成UI组件绑定的代码，在搭建具有很多子物体的UI时可以使用，效果如下：

Inspector界面：

![1](https://github.com/user-attachments/assets/75d2743c-4f57-4160-a930-c8b986208b47)


点按自动绑定后，会在设置好的路径生成以下代码文件，只需要在主代码逻辑类里面声明class 为partial，并且在初始化时调用GetBindComponents(gameobject)就可以自动绑定组件

![2](https://github.com/user-attachments/assets/e9eac5a0-26a4-49d0-b98e-5c740b727b86)


## 使用方法

该组件的结构如下：

![3](https://github.com/user-attachments/assets/16e583ab-bd26-4757-b065-92f76ea925ef)


其中`ComponentAutoBindTool`为挂载在UI上的自动绑定组件。

### 预制体命名规则

该绑定工具必须挂载在UI预制体上面，且需提前创建预制体同名脚本并挂载在预制体上（如图例中的`ScoreModeSettleUIForm`）,生成的代码将是这个类的partial class。

工具根据预制体的名称末尾为Form还是Item区分预制体的UI类型，也就是说界面预制体必须命名为XXXForm，Item同理。

### 需要自动绑定的组件命名规则

![4](https://github.com/user-attachments/assets/7f833945-ca72-4851-92a7-aa843a69449a)


点按支持的缩写规则可以展开详细，举例说明就是：某个button需要自动绑定，就要命名为**`m_btn_XXX`，**某个Text需要自动绑定就要命名为**`m_txt_XXX`**，这样生成好的代码中就会以命名的名称自动绑定该组件。

命名好之后，就可以点击自动绑定按钮，工具就会一键生成代码在预先设置的路径下的自动生成的BindComponents文件夹中，注意点击自动绑定按钮时，**必须预先挂载了该组件的UGuiForm脚本，并且在预制体编辑界面。**

### 路径与命名空间设置

点击后会打开该窗口，可以设置自动生成的代码位置。

![5](https://github.com/user-attachments/assets/4ae1ffe8-1ab7-4116-bb04-71104ebc6865)


虽然看起来前置步骤很多，但是其实熟悉下之后制作功能较复杂的UI会方便很多。

出现bug可以来找我( QQ424504326
