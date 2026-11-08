#include <Windows.h>
#include <sddl.h>
#include <winternl.h>

extern NTSTATUS NTAPI NtOpenSection(PHANDLE SectionHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes);

void start()
{
    // Retrieve the current session ID for the process.
    DWORD sessionId;
    ProcessIdToSessionId(GetCurrentProcessId(), &sessionId);

    // swprintf_s(sectionName, _countof(sectionName), L"\\Sessions\\%lu\\Windows\\ThemeSection", sessionId);
    wchar_t sectionName[42];
    const char* suffixRev = "noitceSemehT\\swodniW\\";
    const char* prefixRev = "\\snoisseS\\";
    unsigned int i = 1;
    sectionName[41] = 0;
    while (*suffixRev)
        sectionName[41 - i++] = *suffixRev++;
    do {
        sectionName[41 - i++] = sessionId % 10 + '0';
        sessionId /= 10;
    } while (sessionId);
    while (*prefixRev)
        sectionName[41 - i++] = *prefixRev++;

    // Define the name of the section object.
    UNICODE_STRING sectionObjectName;
    sectionObjectName.Buffer = sectionName + 42 - i;
    sectionObjectName.Length = (USHORT)((i - 1) * sizeof(WCHAR));
    sectionObjectName.MaximumLength = (USHORT)(i * sizeof(WCHAR));

    // Define the attributes for the section object.
    OBJECT_ATTRIBUTES objectAttributes;
    InitializeObjectAttributes(&objectAttributes, &sectionObjectName, OBJ_CASE_INSENSITIVE, NULL, NULL);

    HANDLE hSection;
    NTSTATUS status = NtOpenSection(&hSection, WRITE_DAC, &objectAttributes);
    if (status) ExitProcess(1);

    const wchar_t* cmd = GetCommandLineW();
    const wchar_t* end = cmd;
    while (*end) ++end;
    BOOL restore = end != cmd && end[-1] == '1';

    // Define your SDDL string.
    LPCSTR sddl = restore ? "D:(A;;CCLCRC;;;IU)(A;;CCDCLCSWRPSDRCWDWO;;;SY)"
        : "D:(A;;RC;;;IU)(A;;DCSWRPSDRCWDWO;;;SY)";
    PSECURITY_DESCRIPTOR psd = NULL;

    // Convert the SDDL string to a security descriptor.
    if (!ConvertStringSecurityDescriptorToSecurityDescriptorA(sddl, 1, &psd, NULL)) {
        ExitProcess(1);
    }

    // Set the security descriptor for the object.
    BOOL result = SetKernelObjectSecurity(
        hSection,
        DACL_SECURITY_INFORMATION,
        psd
    );

    ExitProcess(!result);
}
