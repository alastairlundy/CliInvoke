

# CliInvoke

<!-- Badges -->
[![Latest NuGet](https://img.shields.io/nuget/v/CliInvoke?style=flat-square&label=Latest%20Stable%20Release)](https://www.nuget.org/packages/CliInvoke/)
[![Latest Pre-release NuGet](https://img.shields.io/nuget/vpre/CliInvoke?style=flat-square&label=Latest%20Pre-Release)](https://www.nuget.org/packages/CliInvoke/)
[![Downloads](https://img.shields.io/nuget/dt/CliInvoke?style=flat-square)](https://www.nuget.org/packages/CliInvoke/)
[![GitHub License](https://img.shields.io/github/license/alastairlundy/CliInvoke?style=flat-square)](https://github.com/alastairlundy/CliInvoke/blob/main/LICENSE.txt)
![OpenSSF Scorecard Score](https://img.shields.io/ossf-scorecard/github.com/alastairlundy/CliInvoke?style=flat-square&label=OpenSSF%20Scorecard%20Score)

<img src="https://github.com/alastairlundy/CliInvoke/blob/main/.assets/icon.png" width="192" height="192" alt="CliInvoke Logo">

CliInvoke es una biblioteca de .NET para interactuar con interfaces de línea de comandos y envolver ejecutables.

Inicie procesos, redireccione las entradas y salidas estándar, espere la finalización del proceso y mucho más.

## Tabla de Contenidos

* [Características](#features)
* [Comparación con Alternativas](#comparison-vs-alternatives)
* [Instalación de CliInvoke](#installing-cliinvoke)
    * [Plataformas Compatibles](#supported-platforms)
* [Ejemplos](#examples)
* [Liberación de Recursos](#resource-disposal)
* [Documentación](#documentation)
* [Cómo Contribuir a CliInvoke](#how-to-contribute-to-cliinvoke)
* [Usado Por](#used-by)
* [Hoja de Ruta de CliInvoke](#cliinvokes-roadmap)
* [Licencia](#license)
* [Agradecimientos](#acknowledgements)

## Características

* Separación clara de responsabilidades entre los constructores de configuración de proceso, los modelos de configuración y los invocadores.
* Compatible con .NET 10 y tiene pocas dependencias.
* Cuenta con extensiones de Inyección de Dependencias para facilitar su uso.
* Soporte para especializaciones específicas, como ejecutar ejecutables o comandos a través de Windows PowerShell o CMD en Windows <sup>1</sup>
* Soporte para [SourceLink](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/sourcelink)

<sup>1</sup> Biblioteca de especializaciones distribuida por separado.

## Comparación con Alternativas

| Característica / Criterio                                                        |  CliInvoke  |                                  [CliWrap](https://github.com/Tyrrrz/CliWrap/)                                   |    [ProcessX](https://github.com/Cysharp/ProcessX)     |                             Clase Process de .NET                             |
|----------------------------------------------------------------------------|:-----------:|:----------------------------------------------------------------------------------------------------------------:|:------------------------------------------------------:|:--------------------------------------------------------------------------:|
| Tipos dedicados de constructor, modelo e invocador (separación clara de responsabilidades) |      ✅      |                                                        ❌                                                         |                           ❌                            | ⚠️, ofrece una separación limitada de responsabilidades a través de la clase de modelo ProcessStartInfo |
| Extensiones de registro de Inyección de Dependencias                               |      ✅      |                                                        ❌                                                         |                           ❌                            |                                     ❌                                      |
| Instalable vía NuGet                                                      |      ✅      |                                                        ✅                                                         |                           ✅                            |                            ✅ , integrado en .NET                             |
| Soporte oficial multiplataforma (publicitado: Windows/macOS/Linux/BSD)      |      ✅      |                                                        ✅*                                                        |                           ❌*                           |                                     ✅                                      |  
| Modos de ejecución con y sin búfer                                  |      ✅      |                                                        ✅                                                         |                           ✅                            |           ⚠️, puede provocar interbloqueos o excepciones si no se tiene cuidado           |
| Soporte para tiempo de espera (timeout) de proceso/comando                                        |      ✅      |                              :warning:, limitado a cancelación mediante CancellationToken                              | :warning:, limitado a cancelación mediante CancellationToken |           :warning:, limitado a cancelación mediante CancellationToken           |
| Soporte de cancelación ordenada mediante señales SIGTERM/SIGINT                   |  ✅, 2.3.0+  |                                                        ✅                                                         |                           ❌                            |                                     ❌                                      |
| Superficie pequeña y dependencias mínimas                                |      ✅      |                                                        ✅                                                         |                           ✅                            |                                     ✅                                      |  
| Licencia / términos adicionales del repositorio                                    | ✅ (MPL‑2.0) | ⚠️ (MIT; el proyecto de pruebas referencia una biblioteca con código fuente disponible; el repositorio contiene una declaración informal de "Términos de Uso") |                        ✅ (MIT)                         |                    ✅ (.NET Runtime licenciado bajo MIT)                     |

Notas:

- *Indica que no se publicita explícitamente para todos los SO enumerados, pero puede funcionar en la práctica; consulte la documentación de cada proyecto.
- El repositorio de CliWrap incluye un proyecto de pruebas que referencia una biblioteca con código fuente disponible (no de código abierto); dicha biblioteca se usa solo para pruebas y no se distribuye con el paquete de ejecución. El repositorio también contiene una declaración informal de "Términos de Uso": revise los archivos del repositorio si requiere certeza legal.

## Instalación de CliInvoke

CliInvoke está disponible en [NuGet Gallery](https://nuget.org) pero también puede instalarse mediante la CLI del SDK ``dotnet``.

Los paquetes a instalar dependen de su caso de uso:

* Para uso en una biblioteca .NET: instale el paquete de abstracciones; sus desarrolladores pueden instalar los paquetes de Implementación y Extensiones de DI.
* Para uso en una aplicación .NET: instale el paquete de implementación y el paquete de Extensiones de Inyección de Dependencias.

| Tipo de proyecto / Necesidad                                                          | Paquetes a instalar (dotnet add package ...)                                      | Notas                                                                        |
|------------------------------------------------------------------------------|-----------------------------------------------------------------------------------|------------------------------------------------------------------------------|
| Autor de biblioteca (solo proporcionar abstracciones)                                   | `CliInvoke.Core`                                                                  | Solo el paquete Core (abstracciones); los consumidores pueden elegir las implementaciones. |
| Biblioteca o app que necesita constructores/implementaciones concretas                | `CliInvoke.Core`, `CliInvoke`                                                     | Paquete de implementación más Core para modelos/abstracciones.                    |
| App de escritorio o consola (caso común: usar DI y ayudantes)  | `CliInvoke.Core`, `CliInvoke`, `CliInvoke.Extensions`                             | Incluye registro de DI y extensiones de conveniencia para una configuración fácil.          |
| Cualquier proyecto que necesite especializaciones de plataforma o shell (opcional) | `CliInvoke.Specializations` (instale junto con los paquetes anteriores según sea necesario) | Añade Cmd/PowerShell y otras especializaciones; incluya solo cuando sea requerido.   |

### Enlaces a los paquetes

[CliInvoke.Core Nuget](https://nuget.org/packages/CliInvoke.Core)
[CliInvoke Nuget](https://nuget.org/packages/CliInvoke)
[CliInvoke.Extensions Nuget](https://nuget.org/packages/CliInvoke.Extensions)
[CliInvoke.Specializations Nuget](https://nuget.org/packages/CliInvoke.Specializations)

## Plataformas Compatibles

CliInvoke es compatible con Windows, macOS, Linux, FreeBSD, Android y potencialmente otros sistemas operativos.

Para más detalles, consulte la [lista de plataformas compatibles](site/docs/Supported-OperatingSystems.md)

## Patrones de Diseño y Cuándo Usarlos

CliInvoke proporciona tres patrones de diseño distintos para invocar procesos. Consulte [PATTERNS.md](PATTERNS.md) para obtener documentación exhaustiva sobre cada patrón.

* **`CliRun`** – Punto de entrada amigable para principiantes/rápido. Úselo para scripting básico, tareas de CI/CD o ejecución simple de comandos. Sin código repetitivo, argumentos opcionales con valores predeterminados sensatos.
* **`IProcessInvoker`** – Patrón centrado en DI y soporte para gestión de procesos de extremo a extremo. Úselo cuando construya aplicaciones que necesiten probabilidad, integración con inyección de dependencias o configuración de proceso personalizada por invocación.
* **`IExternalProcess` & `IExternalProcessFactory`** – API similar a proceso con soporte DI, capacidades ricas, comportamiento estable y predecible. Úselo cuando necesite control granular del ciclo de vida, secuencias manuales de inicio/parada o escenarios de usuario avanzado similares a `System.Diagnostics.Process`.

## Ejemplos

### Amigable para Principiantes / Inicio Rápido

Para casos de uso simples, el ayudante `CliRun` proporciona una API directa para ejecutar comandos con el mínimo código repetitivo:

```csharp
using CliInvoke;
using CliInvoke.Core;

// Execute a command and get the result
ProcessResult result = await CliRun.RunAsync("dotnet", "--version");
Console.WriteLine($"Exit Code: {result.ExitCode}");
```

Para capturar la salida, use `RunBufferedAsync`:

```csharp
using CliInvoke;
using CliInvoke.Core;

// Execute and capture stdout/stderr
BufferedProcessResult result = await CliRun.RunBufferedAsync("dotnet", "--info");
Console.WriteLine(result.StandardOutput);
Console.WriteLine(result.StandardError);
```

`CliRun` es ideal para scripting, prototipos rápidos y ejecución básica de comandos donde no necesita inyección de dependencias ni configuración avanzada.

Para documentación detallada sobre todos los patrones disponibles y cuándo usarlos, consulte [PATTERNS.md](PATTERNS.md).

### Configuración Avanzada

Para un control detallado sobre la ejecución de procesos (tiempos de espera personalizados, estrategias de cancelación, salida con/sin búfer y configuración basada en constructores), consulte la **[Guía de Configuración](site/docs/guides/configuration.md)** y la guía **[Elegir su Patrón de Invocación](site/docs/guides/choosing-invocation-pattern.md)** en el portal de documentación.

## Liberación de Recursos

> [!IMPORTANT]
> CliInvoke tiene exactamente **cinco Tipos Propietarios de Recursos** que implementan `IDisposable` y **deben** liberarse después de su uso para evitar fugas de recursos (controladores de tuberías abiertos, controladores del kernel y búferes `SecureString` fijados):
>
> | # | Tipo | Qué posee |
> |---|------|-------------|
> | 1 | `ProcessConfiguration` | `StreamWriter` (StandardInput), `UserCredential` opcional |
> | 2 | `IExternalProcess` | `System.Diagnostics.Process` subyacente (tuberías, controladores, subprocesos) |
> | 3 | `PipedProcessResult` | Flujos `StandardOutput` y `StandardError` |
> | 4 | `UserCredential` | Búfer de contraseña `SecureString` |
> | 5 | `UserCredentialBuilder` | Búfer de contraseña `SecureString` en cola para `Build()` |
>
> Ningún otro tipo de CliInvoke implementa `IDisposable`. Envuelva siempre estos tipos en declaraciones `using` o `await using`.

Para la referencia completa de liberación (reglas de propiedad, patrones de liberación y una lista de verificación), consulte la **[Guía de Liberación de Recursos](site/docs/guides/resource-disposal.md)**.

## Documentación

La documentación completa está disponible en el [Portal de Desarrolladores de CliInvoke](site/docs/readme.md). Elija la ruta que mejor se adapte a usted:

| Quién eres | Comienza aquí |
|---|---|
| **Principiante** — "Solo necesito ejecutar un comando" | [Inicio Rápido](site/docs/getting-started-quickstart.md) → [Elegir su Patrón de Invocación](site/docs/guides/choosing-invocation-pattern.md) |
| **Desarrollador Profesional** — "Estoy construyendo una app probable con DI" | [Primeros Pasos](site/docs/getting-started.md) → [Configuración](site/docs/guides/configuration.md) |
| **Usuario Avanzado** — "Necesito control total del ciclo de vida" | [Elegir su Patrón de Invocación → IExternalProcess](site/docs/guides/choosing-invocation-pattern.md#iexternalprocess--power-user-lifecycle-control) → [Arquitectura](site/docs/guides/architecture.md) |

Otras guías: [Solución de Problemas](site/docs/guides/troubleshooting.md) · [Guías de Migración](site/docs/migration-guides/readme.md) · [Compilación desde Código Fuente](site/docs/building-cliinvoke.md)

## Cómo compilar el código de CliInvoke

Por favor, consulte [building-cliinvoke.md](site/docs/building-cliinvoke.md) para saber cómo compilar CliInvoke desde el código fuente.

## Cómo Contribuir a CliInvoke

Por favor, consulte el [archivo CONTRIBUTING.md](CONTRIBUTING.md) para contribuciones de código y localización.

Si desea reportar un error o sugerir una posible función, consulte la [página de issues de GitHub](https://github.com/alastairlundy/CliInvoke/issues/) para ver si ya existe un problema similar o idéntico abierto.
Si no hay un problema relevante registrado,
por favor [regístrelo aquí](https://github.com/alastairlundy/CliInvoke/issues/new) y siga las instrucciones respectivas de la plantilla de issue correspondiente.

## Usado Por

CliInvoke es utilizado por estos proyectos:

* [WCountLib.Providers.wc](https://github.com/alastairlundy/WCount/tree/main/src/lib/WCountLib.Providers.wc) –
  Implementa WCountLib.Abstractions usando el comando Unix ``wc``.

¿Quiere que su proyecto se añada a esta lista? [Abra un issue](https://github.com/alastairlundy/cliinvoke/issues/new/)

## Hoja de Ruta de CliInvoke

CliInvoke tiene como objetivo facilitar el trabajo con comandos y procesos externos.

Si bien actualmente hay un conjunto modesto de características disponibles, existe margen para más características y modificaciones de las existentes en futuras actualizaciones.

Las actualizaciones futuras pueden centrarse en una o más de las siguientes:

* Mayor facilidad de uso
* Mayor estabilidad
* Nuevas características
* Mejora de características existentes

## Paquete y Espacio de Nombres Nuevo vs. Anterior

CliInvoke cambió su ID de paquete NuGet y su espacio de nombres a partir de la re-publicación de la 2.0.0 (etiquetada como 2.0.0-v2) y desde entonces se ha publicado directamente bajo el prefijo de ID de paquete y espacio de nombres ``CliInvoke``.

Los IDs de paquetes anteriores están marcados como obsoletos y no recibirán actualizaciones futuras.

## Licencia

CliInvoke está licenciado bajo la licencia MPL 2.0. Puede obtener más información al respecto [aquí](https://www.mozilla.org/en-US/MPL/)

Si su proyecto incorpora CliInvoke, asegúrese de que el texto completo del LICENSE.txt de CliInvoke se incorpore en su archivo TXT de licencias de terceros o se proporcione como un archivo TXT distinto dentro del repositorio de su proyecto.

### Activos de CliInvoke

El icono de CliInvoke es propiedad mía y conserva todos los derechos (Alastair Lundy).

Si bifurca CliInvoke y lo redistribuye, reemplace el icono a menos que tenga mi aprobación previa por escrito.

## Historial de Estrellas

<a href="https://www.star-history.com/?repos=alastairlundy%2Fcliinvoke&type=date&logscale=&legend=top-left">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/chart?repos=alastairlundy/cliinvoke&type=date&theme=dark&legend=top-left" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/chart?repos=alastairlundy/cliinvoke&type=date&legend=top-left" />
   <img alt="Star History Chart" src="https://api.star-history.com/chart?repos=alastairlundy/cliinvoke&type=date&legend=top-left" />
 </picture>
</a>

## Agradecimientos

### Proyectos

Este proyecto desea agradecer a los siguientes proyectos por su trabajo:

* [CliWrap](https://github.com/Tyrrrz/CliWrap/) por inspirar este proyecto
* [Polyfill](https://github.com/SimonCropp/Polyfill) por simplificar el soporte de TFMs más antiguos

Para más información, consulte
el [archivo THIRD_PARTY_NOTICES](https://github.com/alastairlundy/CliInvoke/blob/main/THIRD_PARTY_NOTICES.txt).
