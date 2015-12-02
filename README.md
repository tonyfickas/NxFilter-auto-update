# NxFilter-auto-update
Auto update application for NxFilter, this will help reduce downtime on network as well provide scheduled updates from the shallalist

This is for made for: NxFilter

http://www.nxfilter.org/

![nothing](http://www.nxfilter.org/p2/wp-content/uploads/2014/07/rb_logo41.png)  

**Why is this application different than using a .bat file?**
- Check current version of filter list to see if update is required (checks local to server md5 hash before downloading)
- Doesn't required a 3rd party extracting software (nxFilter-auto-update can extract .tar.gz file)
- Reduces downtime by pre-download shalla filter list and extracting it before stopping service and updating it

**Arguments**
- f = force update (if you don't want the md5 hash check)

**Lastest Download**

[Download](https://github.com/bikecrazyy/NxFilter-auto-update/raw/master/nxFilter-auto-update.exe)


**Setup**
- Install nxFilter
- Download nxFilter-auto-update to your nxFilter path c:\nxFilter\
- Schedule task as need under Administrative Tools->Task Scheduler
