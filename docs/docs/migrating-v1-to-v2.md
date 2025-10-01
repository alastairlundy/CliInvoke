# Migrating CliInvoke v1 to v2

CliInvoke v2 contains a number of breaking changes from CliInvoke v1. This document aims to detail alternatives to removed interfaces and classes.

## Invokers

### ``IProcessFactory`` and ``ProcessFactory``

``IProcessFactory`` and ``ProcessFactory`` have been removed in v2.

They have been removed due to exposing behaviour and functionality that is antithetical to the goals of CliInvoke. Safe process running and avoiding the pitfalls of the ``Process`` class are easily done with other invokers but ``IProcessFactory``'s interface methods made it easier to run into some of these pitfalls.

The new ``IProcessInvoker`` in v2 should be used instead.

#### Non Buffered Process Execution


#### Buffered Process Execution


### Original ``IProcessInvoker`` and ``ProcessInvoker``

What was originally called ``IProcessInvoker`` in v1 has been effectively split into 2 new interfaces and their respective classes, ``IProcessInvoker`` and ``IProcessConfigurationInvoker``.

The new ``IProcessInvoker`` deals with ``ProcessStartInfo`` whilst ``IProcessConfigurationInvoker`` deals with ``ProcessConfiguration``.

Which interface (and class) to use depends on whether you want to use CliInvoke's primitives or use ``ProcessStartInfo`` for configuring Processes.

| | ``IProcessInvoker`` | ``IProcessConfigurationInvoker`` |
|-|-|-|
| Process Configuration Primitive | ``ProcessStartInfo`` | ``ProcessConfiguration`` |
| Process Standard Input Redirection |  ``StreamWriter?`` | ``StreamWriter? StandardInput`` in ``ProcessConfiguration`` |
| Process Standard Output and Error Redirection | Supports retrieving them with ``ExecuteBufferedAsync`` and ``ExecutePipedAsync`` methods only. | Can be read to ``StreamReader? StandardOutput`` and ``StreamReader? StandardError`` in ``ProcessConfiguration`` regardless of method choice. Also supports retrieving them with ``ExecuteBufferedAsync`` and ``ExecutePipedAsync`` methods.  | 
| Process Resource Primitives | ``ProcessResourcePolicy`` | ``ProcessResourcePolicy?`` in ``ProcessConfiguration`` |
| Process Timeout Primitives | | |


### ``ICliCommandInvoker`` and ``CliCommandInvoker``


## Primitives

### ``CliCommandConfiguration``