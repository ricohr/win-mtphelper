MTP Access helper for Windows
=============================

How to use
----------

### Download the necessary Files

```
# update submodule
$ git submodule update --init
```

### Execution with pipe

When starting with no command argument, MtpHelper.exe will wait for MTP command from stdin.
Repeat until stdin is closed.

```
$ MtpHelper.exe
deviceList
{"status":"OK","devices":["usb#vid_05ca&pid_0366#00102114#{6ac27878-a6fa-4155-ba85-f98f491d4f33}"]}
desc usb#vid_05ca&pid_0366#00102114#{6ac27878-a6fa-4155-ba85-f98f491d4f33} ExposureBiasCompensation
{"status":"OK","current":0,"values":[2000,1700,1300,1000,700,300,0,-300,-700,-1000,-1300,-1700,-2000]}
get usb#vid_05ca&pid_0366#00102114#{6ac27878-a6fa-4155-ba85-f98f491d4f33} ExposureBiasCompensation
{"status":"OK","current":0}
set usb#vid_05ca&pid_0366#00102114#{6ac27878-a6fa-4155-ba85-f98f491d4f33} ExposureBiasCompensation -2000
{"status":"OK"}
```

### Execution with command argument

If you specify the MTP command as the command argument, MtpHelper.exe will be executed only once.

```
$ MtpHelper.exe deviceList
{"status":"OK","devices":["usb#vid_05ca&pid_0366#00102114#{6ac27878-a6fa-4155-ba85-f98f491d4f33}"]}
```
```
$ MtpHelper.exe set "usb#vid_05ca&pid_0366#00102114#{6ac27878-a6fa-4155-ba85-f98f491d4f33}" ExposureBiasCompensation -2000
{"status":"OK"}
```


Supported commands
------------------

### deviceList

Get a list of connected MTP devices.

```
deviceList
{"status":"OK", "devices":[ Array of DeviceId ]}
```

### deviceInfo

Get device informations of the specified MTP device.
```
deviceInfo DEVICE-ID
{"status":"OK", Hash of device information }
```

### desc

Gets the description of the specified device property.
```
desc DEVICE-ID PROPERTY-NAME
{"status":"OK", Hash of description }
```

### get

Gets the value of the specified device property.
```
get DEVICE-ID PROPERTY-NAME
{"status":"OK","current": Value of property }
```

### set

Sets the value of the specified device property.
```
set DEVICE-ID PROPERTY-NAME VALUE
{"status":"OK"}
```

### sendConfig

** FOR RICOH R ONLY **

Writes the config file to the device.
```
sendConfig DEVICE-ID CONFIG-FILENAME
{"status":"OK"}
```

### getConfig

** FOR RICOH R ONLY **

Read the config file from the device and save it to a file.
```
getConfig DEVICE-ID CONFIG-FILENAME
{"status":"OK"}
```

### firmwareUpdate

** FOR RICOH R ONLY **

Write the firmware file to the device.
```
firmwareUpdate DEVICE-ID FIRMWARE-FILE
{"status":"OK"}
```


Events
------

When a device is plugged / unplugged, an event message is output to the stderr.
```
{"event":"DeviceAdded","deviceId": Plugged DeviceId }
{"event":"DeviceRemoved","deviceId": Unplugged DeviceId }
```


Sample execution from electron
------------------------------

Start ``MtpHelper/bin/Debug/mtphelper.exe`` as a helper.

１. Launch application.
```
$ cd electron-sample
$ npm install
$ ./node_modules/.bin/electron .
```
２. Insert a command in the text box and press ENTER.
```
deviceList
```
３. Results are displayed below the text box.
```
{"status":"OK","devices":["usb#vid_05ca&pid_0366#00102114#{6ac27878-a6fa-4155-ba85-f98f491d4f33}"]}
```


MtpHelper.json 
--------------

This is a config file that MtpHelper.exe refers to.
Place it in the same location as MtpHelper.exe or specify it with ``-conf:JSON_FILENAME``.

Please see here for the specific content. [MtpHelper.json](https://github.com/ricohr/ricoh-r-console/blob/master/lib/mtphelper/MtpHelper.json)

### friendlyNames

An array of device names to search with ``deviceList``.

### properties

Available deviceProperty list.
You can add deviceProperty by increasing entries, but depending on the type you need to deal with MtpHelper.exe.


Contributing
------------

Bug reports and pull requests are welcome on GitHub at https://github.com/ricohr/win-mtphelper .


License
-------

This software is available as open source under the terms of the [MIT License](http://opensource.org/licenses/MIT).
