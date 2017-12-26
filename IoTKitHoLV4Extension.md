# 2017/12/25のセミナーでは 
[第二章](https://1drv.ms/p/s!Aihe6QsTtyqct5NPsPTykYx8VQ6aNw)Step 1で作成したStream AnalyticsとStep 3で作成したWeb Appを改造して、参加者が作成したEvent Hubにデータを転送。 
## Stream Analyticsの改造 
"throughmsg"という名前で、Event Hubを一個作成 
Stream Analyticsのクエリーに以下を追加して、新たに作成したEvent Hubに生データを送信 
```sql
SELECT * INTO throughmsg FROM sensor TIMESTAMP by time
```
これで、Raspberry Pi3が送信してきて、かつ、IoT Hubがプロパティを追加したセンシングデータが、throughmsgに送信される。 

## Web AppのSignalR Hubの改造 
SignalR Hubに、新たに"Message"という名前のメソッドを追加。 
```cs
        public void Message(string msg)
        {
            Clients.Others.Message(msg);
        }
```

## Functionの追加 
Event Hubのthourghmsgに入力バインドするFunctionを追加する。この中で、新たに作ったSignal RのMessageにデータを送る。 

project.json
```json
{
	"frameworks":{
		"net46":{
			"dependencies":{
				"Newtonsoft.Json":"10.0.2",
				"Microsoft.AspNet.SignalR.Client":"2.2.2"
			}
		}
	}
}
```
SignalRにメッセージを送るので。  
run.csx
```cs
using System;

public static async void Run(string myEventHubMessage, TraceWriter log)
{
    log.Info($"C# Event Hub trigger function processed a message: {myEventHubMessage}");
    var hubConnection = new Microsoft.AspNet.SignalR.Client.HubConnection("http://[your-web-site].azurewebsites.net/");
    var proxy = hubConnection.CreateHubProxy("EnvHub");
    await hubConnection.Start();
    proxy.Invoke("Message",myEventHubMessage);
}
```
これで、EnvHubのMessageにデータが転送される。[your-web-site]は、各自のWeb AppのDeployに合わせて変えること。 

## HTMLファイルを追加  
各自のEvent Hubに転送するためのWebページを作成する。 Web AppのVisual Studioのプロジェクトの直下に、TransferMessage.htmlという名前でファイルを追加する。 
```html
<html>
<head>
    <title>Stream Analytics Hands-on Seminar Utility</title>
    <script type="text/javascript" src="Scripts/jquery-3.2.1.min.js"></script>
    <script type="text/javascript" src="Scripts/jquery.signalR-2.2.2.min.js"></script>
    <script type="text/javascript" src="SignalR/hubs"></script>
    <script type="text/javascript">
        function sendMsg(cs, msg) {
            $.ajax({
                url: '/api/SendEH?cs=' + encodeURIComponent(cs),
                type: 'POST',
                data: msg,
                success: function () {
                    //
                },
                error: function (XMLHttpRequest, textStatus, errorThrown) {
                    alert(textStatus);
                }
            });
        }
        function connect() {
            var csItem = document.getElementById("csItem");
            var dmItem = document.getElementById("dmItem");
            var selected = dmItem.selectedIndex;
            var csValue = csItem.value;
            if (csValue.indexOf("Endpoint") == -1 || csValue.indexOf("SharedAccessKeyName") == -1 && csValue.indexOf("SharedAccessKey") == -1 && csValue.indexOf("EntityPath") == -1) {
                alert("Please input valid connection string for Event Hub!");
                return;
            }
            var countItem = document.getElementById("idCount");
            var count = 0;
            var hub = $.connection.envHub;
            if (selected == 0) {
                hub.on("Message", function (msg) {
                    var jsonObj = JSON.parse(msg);
                    var sensorArray = [
                        { 'sensortype': 'ambience', 'sensorvalue': jsonObj.ambience },
                        { 'sensortype': 'humidity', 'sensorvalue': jsonObj.humidity },
                        { 'sensortype': 'pressure', 'sensorvalue': jsonObj.pressure }
                    ];
                    jsonObj.measurements = sensorArray;
                    var emsg = JSON.stringify(jsonObj);
                    sendMsg(csValue, emsg);
                    count++;
                    countItem.textContent = "send:" + count;
                });
            }
            else if (selected == 1) {
                hub.on("Geolocation", function (msg) {
                    sendMsg(csValue, msg);
                });
            }
            $.connection.hub.start();
        }
    </script>
</head>
<body>
    <table>
        <tr><td><input id="csItem" type="text" value="please input event hub connection string..."/></td></tr>
        <tr><td><input type="button" value="Connect" onclick="connect()"/></td></tr>
        <tr><td>
            <select id="dmItem">
                <option value="0">sensor</option>
                <option value="1">geolocation</option>
            </select>
        </td></tr>
        <tr><td><p id="idCount"/></td></tr>
    </table>
</body>
</html>
```
Event Hubの接続文字列をセットして、Connectボタンをクリックすると、SignalHubのEnvHubにSubscribeして、Messageを待つ。 
本当は、そのまま、各自のEvent Hubにメッセージを送信したかったのだが、どうも、Event Hubはクロスドメインを禁止しているようでできなかったので、Web AppにREST APIを追加して、そのAPIにPOSTするようにしている。 
## Web AppへのREST APIの追加 
Web AppのVisual Studioプロジェクトで、Controllersフォルダーを右クリックし、スキャッフォールディングを使って、コントローラをSendEHControllerという名前で追加する。 
NuGetで、WindowsAzure.ServiceBus SDKをインストールし、以下のロジックでPostメソッドを追加する。 
```cs
        public async Task<HttpResponseMessage> Post([FromUri] string cs)
        {
            try
            {
                string msg = await this.Request.Content.ReadAsStringAsync();
                var eh = EventHubClient.CreateFromConnectionString(cs);
                await eh.SendAsync(new EventData(Encoding.UTF8.GetBytes(msg)));
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch(Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }
```
TransferMessage.html内の$.ajaxコールは、このロジックをコールすることになる。結果として各自のEvent Hubの接続文字列を使って、各自のEvent Hubにデータ転送されるわけ。 
他に、TransferMessage.htmlは、課題の配列分解用のJSONの配列データを追加してます。これなら、この処理はHTMLを開いているクライアント側で行われるので、Web App側ではその分のCPUロードを減らせるわけだ。 
TransferMessage.htmlを開くブラウザが増えれば増えるほど、Subscribeしているノードが増えて、Web AppのSignalRの転送負担が増えていくことになり、能力的に厳しくなったら、Web AppのApp Serviceをスケールアップすればよい。（因みに、セミナー当日は40人近くの参加者がいて、Freeだと賄えず、Basicにあげた） 

最後に、データの流れを書いておくので、参考にしてください。 
[CC2650→(BLE)→Raspberry Pi 3(IoT Edge SDK Ver1)]→Azure IoT Hub→Stream Analytics→Event Hub(throughmsg)→Function→SignalR Hub(Message)→TransferMessage.htmlのコールバック（接続文字列を追加）→SendEHController→各自のEvent Hub 

この流れの中で、接続数、メッセージ数、クライアント数によってどこがどう増減するか考えてみてくださいね。基本、金に糸目をつけなければ（素直にスケールアップするってこと）、数千万台のデバイスをつないでも対応可能。 
