# ANSWER  
## Step 1.  
### 1-1.
```sql
SELECT
    IoTHub.ConnectionDeviceId as DeviceId,
    time,
    ambience as temperature,
    humidity, pressure
INTO
    storage
FROM
    sensor
```

### 1-2. 
```sql
SELECT
    IoTHub.ConnectionDeviceId as DeviceId,
    time,
    ambience as temperature,
    humidity, pressure,
    0.81 * ambience + 0.01 * humidity * (0.99 * ambience - 14.3) + 46.3
    AS discomfortIndex
INTO
    storage
FROM
    sensor
```
User Define Function 
```javascript
function main(temperature, humidity) {
    return 0.81 * temperature + 0.01 * humidity * (0.99 * temperature - 14.3) + 46.3;
}
```
```sql
SELECT
    IoTHub.ConnectionDeviceId as DeviceId,
    time,
    ambience as temperature,
    humidity, pressure,
    udf.CalcDiscomfort(ambience, humidity)
    AS discomfortIndex
INTO
    storage
FROM
    sensor
```

### 1-3. 
```sql
WITH WithDevId AS (
SELECT
    IoTHub.ConnectionDeviceId as deviceId,
    time,
    ambience as temperature,
    humidity, pressure
FROM sensor TIMESTAMP by time
)
SELECT deviceId, 
    AVG(temperature) as AvgTemp,
    MAX(temperature) as MaxTemp,
    MIN(temperature) as MinTemp
INTO storage
FROM WithDevId
GROUP BY TUMBLINGWINDOW(minute,3), deviceId
```

### 1-4. 
```sql
SELECT
    IoTHub.ConnectionDeviceId as DeviceId, *
INTO
    storage
FROM
    sensor
WHERE temperature > 30
```
## Step 2 
### 2-1. 
```sql
SELECT
    s.IoTHub.ConnectionDeviceId as deviceId,
    s.time,
    s.ambience as temperature,
    s.humidity, s.pressure,
    udf.CalcDiscomfort(s.ambience, s.humidity)
    AS discomfortIndex,
    r.sensortag, r.maker, r.room, r.description
INTO    storage
FROM    bsensor s TIMESTAMP by time
```

## Step 3 
### 3-1. 
```sql
WITH AddMsgId AS (
    SELECT IoTHub.ConnectionDeviceId as DeviceId,
    CONCAT(IoTHub.ConnectionDeviceId, CAST(time as nvarchar(max))) as msgId,
    *
    FROM sensor
)

SELECT *,
    f.ArrayValue.sensortype as sensortype,
    f.ArrayValue.sensorvalue as sensorvalue
INTO storage
FROM AddMsgId s
CROSS APPLY GetArrayElements(s.measurements) AS f
```