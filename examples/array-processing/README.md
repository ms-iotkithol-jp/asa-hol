# 配列を含むテレメトリーデータを扱う  
IoT Hub を通じて受信するテレメトリーデータに配列が含まれ、かつ、配列の各要素が、メッセージの種類によって異なるケースに対応する。  
IoT Hub から送られてくる JSON データーは以下の様な形式で、配列の数は、メッセージプロパティに付与された、msgType の値によって変わり、要素の意味も変わるとする。  
``` json
{
    "dataItems":[2,8,9],
    "timestamp":"2020/08/26T10:29:54"
}
```  
msgType は、以下の三種類で、それぞれの配列の要素数と意味は以下の通り。 

- type-a - 要素数3で、0:accelx, 1:accely, 2:accelz 
- type-b - 要素数3で、0:gyrox, 1:gyroy, 2:gyroz 
- type-c - 要素数5で、0:temperature, 1:humidity, 2:pressure, 3:brightness, 4:co2 

Stream Analytics で、msgTypeを元に、それぞれのデータを意味付け、type-a、type-b、type-c毎にそれぞれ違う出力先に、意味付けしたデータを出力する。  

## Device のシミュレーション  
./device-simulator の Visual Studio Solution を Visual Studio 2019 で開き、Azure IoT Hub にデバイス登録をして、 app.config にデバイスの接続文字列を追加し実行する。  

## Stream Analytics  
Device のシミュレーションアプリのメッセージの送り先の IoT Hub を
-  '<b>iothub</b>' 

というエリアスで入力を作成する。  
予め Storage Account を作成し、Blob Container を3つ作成し、それぞれを以下のエリアスで 出力を3つ作成する。  
- msgtypea - type-a のメッセージの出力先 
- msgtypeb - type-b のメッセージの出力先
- msgtypec - type-c のメッセージの出力先 

ユーザー定義関数を使って、メッセージの msgType プロパティの指定に従って、配列を意味づけて、それぞれの出力先に流す。そのためのユーザー定義関数は、  
名前：<b>parseDataItem</b>
```javascript
function main(msgType, dataItems) {
    var parsedItems = {};
    if (msgType=='type-a') {
        parsedItems.accelx = dataItems[0];
        parsedItems.accely = dataItems[1];
        parsedItems.accelz = dataItems[2];
    } else if (msgType=='type-b') {
        parsedItems.gyrox = dataItems[0];
        parsedItems.gyroy = dataItems[1];
        parsedItems.gyroz = dataItems[2];
    } else if (msgType=='type-c') {
        parsedItems.temperature = dataItems[0];
        parsedItems.humidity = dataItems[1];
        parsedItems.pressure = dataItems[2];
        parsedItems.brightness = dataItems[3];
        parsedItems.co2 = dataItems[4];
    }

    return parsedItems;
}
```
この関数を使って、以下の様なクエリーを定義する。 
```sql
WITH ParseMsgType AS (
    SELECT
        *,
        GetMetadataPropertyValue(iothub,'[User].[msgType]') as msgtype, 
        UDF.parseDataItem(GetMetadataPropertyValue(iothub,'[User].[msgType]'),dataItems) as parsedDataItem
    FROM
        iothub
)

-- output to type-a message
Select * 
INTO msgtypea
FROM ParseMsgType
WHERE msgtype='type-a'

-- output to type-b message
Select * 
INTO msgtypeb
FROM ParseMsgType
WHERE msgtype='type-b'

-- output to type-c message
Select * 
INTO msgtypec
FROM ParseMsgType
WHERE msgtype='type-c'
```
