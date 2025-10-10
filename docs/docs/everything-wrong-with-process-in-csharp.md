## Everything wrong with the Process class in C#

**Note**: This is not an exhaustive list.

The following things in the Process cause Exceptions, in my and some other people's views, unnecessarily:
* Trying to figure out if a Process ``HasExited`` when it hasn't even started
* Trying to figure out if a Process Has Started by looking at the ``StartTime`` property
