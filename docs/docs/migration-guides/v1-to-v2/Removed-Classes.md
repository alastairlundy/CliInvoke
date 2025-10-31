# Removed Interfaces and Classes
CliInvoke v2 removes a number of interfaces and classes from v1. Below is a non-exhaustive list of the main removed interfaces and classes.

## ``IProcessRunnerUtility`` and ``ProcessRunnerUtility``
**Rationale**:
* Although well intentioned, the interface and classes were badly designed, exposed most of the shortcomings of .NET's ``Process`` class, and were neither helpful to CliInvoke's design nor were they helpful to users.

**Replacement**:
* There are no direct replacements for the reasons stated in the rationale section. Use ``IProcessInvoker`` and ``ProcessInvoker`` instead.

## ``IPipedProcessRunner`` and ``PipedProcessRunner``
**Rationale**:  
* Although well intentioned, the interface and classes were badly designed, and are no longer needed as a middleman between Process Invokers and Process ProcessRunnerUtility. It contributed to a more fragmented and confused API surface and thus removal was necessary to clean up the CliInvoke v2 API Surface.

**Replacement**: 
* ``IProcessInvoker`` and ``ProcessInvoker`` for running ``ProcessConfiguration`` objects.


## ``IProcessFactory`` and ``ProcessFactory``
**Rationale**:  Removed due to exposing behaviour and functionality that is antithetical to the goals of CliInvoke. Safe process running and avoiding the pitfalls of the ``Process`` class are easily done with other invokers and ``IProcessFactory``'s interface methods made it easier to run into some of these pitfalls.

**Replacement**: 
* ``IProcessConfigurationFactory`` and ``ProcessConfigurationFactory`` for simple ``ProcessConfiguration`` creation.
* ``IProcessInvoker`` and ``ProcessInvoker`` for running ``ProcessConfiguration`` objects.

## ``ICliCommandInvoker`` and ``CliCommandInvoker``
**Rationale**: 
* The abstractions around CliInvoke have become less Command centric and more external process centric and as such a name change was warranted. Alongside this, breaking changes were needed for new features and so ``IProcessInvoker ``and ``ProcessInvoker`` were introduced as the successor to `ICliCommandInvoker`` and ``CliCommandInvoker`` respectively.


**Replacement**: 
* ``IProcessInvoker`` and ``ProcessInvoker`` respectively


## ``CliCommandConfiguration``

**Rationale**:
* The abstractions around CliInvoke have become less Command centric and more external process centric and as such a name change was warranted. Alongside this, some breaking changes were needed for several features and so ``ProcessConfiguration `` was introduced as the successor to ``CliCommandConfiguration``.

**Replacement**:
* ``ProcessConfiguration``