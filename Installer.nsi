; ═══════════════════════════════════════════════════════════════════════════
; CBM LRV Estoque — Script de Instalação NSIS 3
; ─────────────────────────────────────────────────────────────────────────
; Autor  : Miguel Nischor <miguel@nischor.com.br>
; Empresa: Corpo de Bombeiros Militar de Lucas do Rio Verde
;
; Pré-requisito — publicar ambas as arquiteturas antes de compilar:
;   dotnet publish -c Release -p:Platform=x86
;   dotnet publish -c Release -p:Platform=x64
;
; Compilar:
;   makensis Installer.nsi
;
; Saída:
;   Build\Installer\CBMLRVEstoque_Setup_1.0.0.exe
; ═══════════════════════════════════════════════════════════════════════════

; ─── Metadados ───────────────────────────────────────────────────────────
!define APP_NAME      "CBM LRV Estoque"
!define APP_SAFE      "CBMLRVEstoque"
!define APP_VERSION   "1.0.0"
!define APP_PUBLISHER "Miguel Nischor"
!define APP_URL       "https://github.com/mgnischor/cbm-lrv-estoque"
!define APP_EXE       "cbm-lrv-estoque.exe"

; Chave de registro para Adicionar/Remover Programas
!define REG_UNINST "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_SAFE}"

; Diretório de dados da aplicação (runtime — expandido pelo Windows)
!define DATA_DIR "$LOCALAPPDATA\controle-estoque"

; Binários publicados (relativos à raiz do projeto)
!define SRC_X86 "Build\Publish\Release\x86\${APP_EXE}"
!define SRC_X64 "Build\Publish\Release\x64\${APP_EXE}"

; ─── Criar pasta de saída em tempo de compilação ─────────────────────────
!system 'cmd /c if not exist "Build\Installer" mkdir "Build\Installer"'

; ─── Configurações gerais ────────────────────────────────────────────────
Name              "${APP_NAME} ${APP_VERSION}"
OutFile           "Build\Installer\${APP_SAFE}_Setup_${APP_VERSION}.exe"
Unicode           True
SetCompressor     /SOLID lzma
SetCompressorDictSize 32
RequestExecutionLevel admin
ShowInstDetails   show
ShowUnInstDetails show

; ─── Modern UI 2 ─────────────────────────────────────────────────────────
!include "MUI2.nsh"
!include "x64.nsh"
!include "LogicLib.nsh"
!include "WinVer.nsh"

!define MUI_ICON   "Resource\Icon\icon.ico"
!define MUI_UNICON "Resource\Icon\icon.ico"
!define MUI_ABORTWARNING

; ─── Páginas — Instalação ────────────────────────────────────────────────
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "LICENSE.md"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES

!define MUI_FINISHPAGE_RUN           "$INSTDIR\${APP_EXE}"
!define MUI_FINISHPAGE_RUN_TEXT      "Iniciar ${APP_NAME} agora"
!define MUI_FINISHPAGE_LINK          "Repositório do projeto no GitHub"
!define MUI_FINISHPAGE_LINK_LOCATION "${APP_URL}"
!insertmacro MUI_PAGE_FINISH

; ─── Páginas — Desinstalação ─────────────────────────────────────────────
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; ─── Idioma (PT-BR) ──────────────────────────────────────────────────────
!insertmacro MUI_LANGUAGE "PortugueseBR"

; ─── Informações de versão do instalador ─────────────────────────────────
VIProductVersion "${APP_VERSION}.0"
VIAddVersionKey /LANG=1046 "ProductName"     "${APP_NAME}"
VIAddVersionKey /LANG=1046 "ProductVersion"  "${APP_VERSION}"
VIAddVersionKey /LANG=1046 "CompanyName"     "${APP_PUBLISHER}"
VIAddVersionKey /LANG=1046 "LegalCopyright"  "Copyright © 2026 ${APP_PUBLISHER}"
VIAddVersionKey /LANG=1046 "FileDescription" "Instalador — ${APP_NAME}"
VIAddVersionKey /LANG=1046 "FileVersion"     "${APP_VERSION}"

; ═══════════════════════════════════════════════════════════════════════════
; SEÇÃO 1 — Programa Principal (obrigatória)
; ═══════════════════════════════════════════════════════════════════════════
Section "Programa Principal" SecMain
  SectionIn RO  ; impede que o usuário desmarque esta seção

  SetOutPath "$INSTDIR"

  ; Copiar executável adequado à arquitetura detectada em runtime
  ${If} ${RunningX64}
    File /oname=${APP_EXE} "${SRC_X64}"
  ${Else}
    File /oname=${APP_EXE} "${SRC_X86}"
  ${EndIf}

  ; Desinstalador
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  ; Atalhos no Menu Iniciar (todos os usuários)
  SetShellVarContext all
  CreateDirectory "$SMPROGRAMS\${APP_NAME}"
  CreateShortcut "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk" \
    "$INSTDIR\${APP_EXE}" "" "$INSTDIR\${APP_EXE}" 0 \
    SW_SHOWNORMAL "" "Sistema de controle de estoque — ${APP_PUBLISHER}"
  CreateShortcut "$SMPROGRAMS\${APP_NAME}\Desinstalar ${APP_NAME}.lnk" \
    "$INSTDIR\Uninstall.exe"

  ; Registrar em Adicionar/Remover Programas
  WriteRegStr   HKLM "${REG_UNINST}" "DisplayName"         "${APP_NAME}"
  WriteRegStr   HKLM "${REG_UNINST}" "DisplayVersion"       "${APP_VERSION}"
  WriteRegStr   HKLM "${REG_UNINST}" "Publisher"            "${APP_PUBLISHER}"
  WriteRegStr   HKLM "${REG_UNINST}" "UninstallString"      '"$INSTDIR\Uninstall.exe"'
  WriteRegStr   HKLM "${REG_UNINST}" "QuietUninstallString" '"$INSTDIR\Uninstall.exe" /S'
  WriteRegStr   HKLM "${REG_UNINST}" "InstallLocation"      "$INSTDIR"
  WriteRegStr   HKLM "${REG_UNINST}" "URLInfoAbout"         "${APP_URL}"
  WriteRegDWORD HKLM "${REG_UNINST}" "NoModify"             1
  WriteRegDWORD HKLM "${REG_UNINST}" "NoRepair"             1
SectionEnd

; ═══════════════════════════════════════════════════════════════════════════
; SEÇÃO 2 — Atalho na Área de Trabalho (opcional, marcada por padrão)
; ═══════════════════════════════════════════════════════════════════════════
Section "Atalho na Área de Trabalho" SecDesktop
  SetShellVarContext current  ; atalho apenas para o usuário que instalou
  CreateShortcut "$DESKTOP\${APP_NAME}.lnk" \
    "$INSTDIR\${APP_EXE}" "" "$INSTDIR\${APP_EXE}" 0 \
    SW_SHOWNORMAL "" "Sistema de controle de estoque — ${APP_PUBLISHER}"
SectionEnd

; ─── Descrições exibidas na página de componentes ────────────────────────
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SecMain}    "Instala o executável principal, cria atalhos no Menu Iniciar e registra o programa no Painel de Controle."
  !insertmacro MUI_DESCRIPTION_TEXT ${SecDesktop} "Cria um atalho na Área de Trabalho do usuário atual."
!insertmacro MUI_FUNCTION_DESCRIPTION_END

; ═══════════════════════════════════════════════════════════════════════════
; CALLBACK — Inicialização do instalador
; ═══════════════════════════════════════════════════════════════════════════
Function .onInit
  ; ── Verificar Windows 10 ou superior ─────────────────────────────────
  ${IfNot} ${AtLeastWin10}
    MessageBox MB_OK|MB_ICONSTOP \
      "${APP_NAME} requer Windows 10 ou versão superior.$\r$\nA instalação será cancelada."
    Abort
  ${EndIf}

  ; ── Detectar instalação anterior ─────────────────────────────────────
  ReadRegStr $0 HKLM "${REG_UNINST}" "DisplayVersion"
  ${If} $0 != ""
    MessageBox MB_YESNO|MB_ICONINFORMATION \
      "O ${APP_NAME} versão $0 já está instalado.$\r$\nDeseja continuar e substituir pela versão ${APP_VERSION}?" \
      IDYES ContinueInstall
    Abort
    ContinueInstall:
  ${EndIf}

  ; ── Pasta padrão conforme arquitetura ────────────────────────────────
  ${If} ${RunningX64}
    StrCpy $INSTDIR "$PROGRAMFILES64\${APP_NAME}"
  ${Else}
    StrCpy $INSTDIR "$PROGRAMFILES32\${APP_NAME}"
  ${EndIf}
FunctionEnd

; ═══════════════════════════════════════════════════════════════════════════
; DESINSTALAÇÃO
; ═══════════════════════════════════════════════════════════════════════════
Section "Uninstall"
  ; Remover arquivos
  Delete "$INSTDIR\${APP_EXE}"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir  "$INSTDIR"

  ; Remover atalhos do Menu Iniciar (todos os usuários)
  SetShellVarContext all
  Delete "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk"
  Delete "$SMPROGRAMS\${APP_NAME}\Desinstalar ${APP_NAME}.lnk"
  RMDir  "$SMPROGRAMS\${APP_NAME}"

  ; Remover atalho da Área de Trabalho (usuário atual)
  SetShellVarContext current
  Delete "$DESKTOP\${APP_NAME}.lnk"

  ; Remover entrada do Painel de Controle
  DeleteRegKey HKLM "${REG_UNINST}"

  ; Oferecer remoção do banco de dados
  IfFileExists "${DATA_DIR}\*.*" 0 SkipData
    MessageBox MB_YESNO|MB_ICONQUESTION \
      "Deseja remover também o banco de dados localizado em:$\r$\n${DATA_DIR}$\r$\n$\r$\nEsta operação é irreversível." \
      IDNO SkipData
    RMDir /r "${DATA_DIR}"
  SkipData:
SectionEnd
