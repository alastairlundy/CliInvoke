# Security Policies
This policy is adapted from [StreamLink's SECURITY POLICY](https://github.com/streamlink/streamlink/blob/master/SECURITY.md)

## Where to report a Vulnerability
**Please do not report a vulnerability via GitHub Issues, discussions, Pull Requests, or any other public facing text or method of communication.**

To report a vulnerability privately, use the **GitHub Private Vulnerability Reporting** feature. To do this navigate to the **Security** tab on this repository, select **Advisories**, and click on **Report a vulnerability**. This creates a private draft vulnerability where you can collaborate with the maintainers. 

## How to report a vulnerability
To keep triage efficient and to ensure maintainer time is focused on fixing actionable vulnerabilities, all reports must adhere to the following criteria:

1. **Proof-of-concept required**  
   Reports containing output from static analysis tools, security scanners, or automated LLMs / AI models will be closed without action unless accompanied by a working, human-verified proof-of-concept demonstrating real-world impact.

2. **Active participation**  
   Security reporters are expected to participate during the triage phase by clarifying findings, confirming reproduction steps, or testing proposed patches on supported environments.

3. **Credit & attribution**  
   Primary reporter credit on associated GHSAs/CVEs is awarded to researchers who actively assist throughout the disclosure process. If a submitter is unable or unwilling to review proposed fixes or answer clarifying technical questions, reporter credit may be withheld or assigned to the maintaining team.


## Preferred information in reports
To help the maintainers triage your report quickly, please keep reports **concise, factual, and straight to the point**. Avoid generic security disclaimers, lengthy AI-generated explanations, or unedited tool outputs.

Please focus on the following details:

1. The type of issue
2. The affected versions
3. Step-by-step instructions or code to deterministically reproduce the issue
4. A list of affected components, API endpoints, etc.
5. The potential impact or threat model demonstrating how an attacker could exploit the issue
6. Optionally, any proposed mitigations or patches you've developed yourself

## Disclosure Policy
The maintainers kindly ask reporters to adhere to responsible disclosure guidelines and keep details private until the advisory details have been published and a public patch is released.

When a valid report is received:
* Maintainers will confirm the issue and determine affected versions
* A fix will be developed and verified privately
* A patched release and a GitHub Security Advisory (with an optional CVE assignment) will be issued

## Patched Versions Policies
Patches for accepted vulnerabilities will only be created for currently supported versions. In rare cases the maintainers may, at their sole discretion, decide to release patches for no longer supported versions in addition to currently supported versions.

### Currently Supported Versions
Versions that are currently supported with Bug Fixes and/or Security Fixes are displayed in the table below.

| Version series | Versions Supported with Bug Fixes | Versions Supported with Security Fixes |
|--| --|--|
| 3.x | 3.0, 3.1 | 3.0, 3.1 |
| 2.x | 2.11 | 2.11 |
| 1.x | :x: | :x: |

### Version Support EOL dates
| Version line | End of Security Fixes |
|-|-|
| 3.0 | December 1st 2026 |
| 2.11 | December 1st 2026 |
