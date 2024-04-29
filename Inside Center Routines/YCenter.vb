Sub Main()

' Y+Y- Inside Probing Script
' Author verser
' vers.by - touch probes, tool setters and precision home switches
' Modified by mikejr83 to work with cheap BL-MachUSB boards


Dim FRate1, FRate2
Dim DMax, Latch, EdgeLength
Dim XYclearance
Dim AutoZeroFlag
Dim ToolNo
Dim ToolD
Dim ProbeD
Dim Zdepth
Dim XHit, YHit, ZHit
Dim XbHit, YbHit

Dim Zpos, Ypos

Dim XScale, YScale, ZScale

Dim AbsIncFlag

Dim CurrentFeed

Dim DiagRes

DiagRes = MachMsg("HAVE YOU PLUGGED IN PROBE?!", "PROBE CHECK", 1)

If DiagRes = 2 Then
	Exit Sub
End If

'Init vars
FRate1 = Abs(GetUserDRO(1821))
FRate2 = Abs(GetUserDRO(1822))
DMax = Abs(GetUserDRO(1823))
ToolNo = GetCurrentTool()
ToolD = GetToolParam(ToolNo,1)
   If GetUserDRO(1829) = 0 Then	
   	ProbeD = ToolD
	Else 
	ProbeD = GetUserDRO(1829)
   End If
Latch = Abs(GetUserDRO(1825))
XYclearance = GetUserDRO(1826)
EdgeLength = GetUserDRO(1828)
Zdepth = GetUserDRO(1830)

If GetOEMLED(1871) Then
AutoZeroFlag=1
Else
AutoZeroFlag=0
End If

AbsIncFlag = GetOEMLED(49)   ' Get the current G91 state

'Temporary save all Axis Scale factors
XScale = GetOEMDRO(59)
YScale = GetOEMDRO(60)
ZScale = GetOEMDRO(61)

'Set All Axis' Scale to 1
SetOEMDRO(59,1)
SetOEMDRO(60,1)
SetOEMDRO(61,1)
Sleep(250)


'Check for Errors

If GetOemLED(16)<>0 Then ' Check for Machine Coordinates
Message "Please change to working coordinates"
SetOEMDRO(59,XScale)
SetOEMDRO(60,YScale)
SetOEMDRO(61,ZScale)
Sleep(250)
Exit Sub ' Exit if in Machine Coordinates
End If

If GetOemLED(825)<>0 Then
Message "Probe is active! Check connection and try again"
Call SetOEMDRO(59,XScale)
Call SetOEMDRO(60,YScale)
Call SetOEMDRO(61,ZScale)
Sleep(250)
Exit Sub ' Exit if probe is tripped
End If


CurrentFeed = GetOEMDRO(818) 'FeedRate()

'main working
	Dim Yplus, Yminus
	'Save Z start position
	Zpos = GetOEMDRO(802)
	Ypos = GetOEMDRO(801)
	'Safe Go to Y+ start position
	'If Not SafeMoveY((-XYclearance+EdgeLength),CurrentFeed) Then
	'	PushMSG("Manually return to the starting position and repeat the search")
	'	Exit Sub 
	'End If
	'If Not SafeMoveZ((-Zdepth),CurrentFeed) Then 
	'	PushMSG("Manually return to the starting position and repeat the search")
	'	Exit Sub 
	'End If
	'Probe Y+
	YHit=ProbeY(1,DMax,Latch,FRate1,FRate2)
	Yplus = YHit - Ypos
	If YHit=999999 Then 
		Exit Sub 
	End If
	'Indicate result
	SetUserLabel (8, Format(YHit+ProbeD/2, "####0.000"))
	'Move to Y+ start position
	If Not SafeMoveY(-1*(GetOEMDRO(801)-Ypos),CurrentFeed) Then 
		PushMSG("Return to the search position is interrupted")
		Exit Sub 
	End If
	'Probe Y-
	YbHit=ProbeY(-1,DMax,Latch,FRate1,FRate2)
	If YbHit=999999 Then 
		Exit Sub 
	End If
	'Indicate result
	SetUserLabel (6, Format(YbHit-ProbeD/2, "####0.000"))
	SetUserLabel (7, Format((YbHit+YHit)/2, "####0.000"))
	SetUserLabel (9, Format(Abs(YbHit-YHit-ProbeD), "####0.000"))
	PushMSG("Y+ = " & (YHit+ProbeD/2) & ", Yc = " & (YbHit+YHit)/2 & ", Y- = " & YbHit-ProbeD/2 & ", Ly = " & Abs(YbHit-YHit-ProbeD))
	'Safe back to start position
	'If Not SafeMoveZ((Zdepth),CurrentFeed) Then 
	'	PushMSG("Return to the search position is interrupted")
	'	Exit Sub 
	'End If
	'Move to Center point
	If Not SafeMoveY((YbHit+YHit)/2-GetOEMDRO(801),CurrentFeed) Then 
		PushMSG("Return to the search position is interrupted")
		Exit Sub 
	End If


	If AutoZeroFlag = 1 Then 
		SetOEMDRO(801, 0) 
		Sleep(150)
	End If

'epilog

SetOEMDRO(59,XScale)
SetOEMDRO(60,YScale)
SetOEMDRO(61,ZScale)
Sleep(250)

Call SetLED49(AbsIncFlag)
SetOEMDRO(818,CurrentFeed) 'SetFeedRate(CurrentFeed)
Sleep(125)

End Sub


'Functions

Function ProbeY(Dir,DMax,Latch,FRate1,FRate2)
	Dim Res
    	Dim Ystart
    	Dim Yinitial
	Dim Ftmp
	Dim AbsIncF
	Dim Hit
	
	ProbeY=999999
	Ystart = GetDRO(1)
	Yinitial = GetOEMDRO(801)
	SetOEMDRO(819, 0)
	Sleep(125)
	
	Ftmp = FeedRate()
	AbsIncF=GetOEMLED(49)
	'Fast Probe Y
	Code "G91"
	Code "F" & FRate1
	Sleep(125)
	Call WaitProbeReady()	
	Code "G31 Y" & (Dir*DMax)
	While IsMoving()
	Wend
	
	Res = GetVar(2001)
	Hit = GetOEMDRO(801)
'	PushMSG("Res=" & Res &", Ystart=" & Ystart & ", DMax=" & DMax & ", FRate1=" & FRate1 & ", Ftmp=" & Ftmp)
	If Abs(Res - Ystart - Dir*DMax) < 0.01 Then
		PushMSG("Error: G31 Y search finished without making contact")
		PushMSG("Manually return to the starting position and repeat the search")
		Call SetLED49(AbsIncF)
		SetOEMDRO(818,Ftmp)
		Sleep(125)
		Exit Function 
	End If 
	'Move back
	Code "G00 Y" & -Dir*Latch
	While IsMoving()
	Wend
	Call WaitProbeReady()	
	'Latch Probe Y
	Ystart = GetDRO(1)
	Code "F" & FRate2
	Sleep(125)
	Code "G31 Y" & Dir*Latch*2
	While IsMoving()
	Wend
	
	'Save result
	Res = GetVar(2001)
	Hit = GetOEMDRO(801)
	If Abs(Res - Ystart - Dir*Latch*2) < 0.01 Then
		PushMSG("Error: G31 Y latch finished without making contact")
		PushMSG("Manually return to the starting position and repeat the search")
		Call SetLED49(AbsIncF)
		SetOEMDRO(818,Ftmp)
		Sleep(125)
		Exit Function 
	End If 
	Code "G00 Y" & -Dir*Latch
	While IsMoving()
	Wend
	Call SetLED49(AbsIncF)
	SetOEMDRO(818,Ftmp)
	Sleep(125)
	ProbeY=Hit
End Function

Function SafeMoveY(Y1, F1) As Boolean 'return 1 (error) if probe tripped
    Dim Ystart
	Dim Ftmp
	Dim AbsIncF
	SafeMoveY=True
	Ystart = GetOEMDRO(801)
	Ftmp = FeedRate()
	AbsIncF=GetOEMLED(49)
	Code "G91"
	Code "F" & F1
	Sleep(125)
	Call WaitProbeReady()	
	Code "G31 Y" & Y1
	While IsMoving()
	Wend
	YHit = GetVar(2001)
	Call SetLED49(AbsIncF)
	If Abs(YHit - Ystart - Y1) > 0.01 Then
		SafeMoveY=False
		PushMSG("Error! Probe tripped during Y movement")
	End If 
	SetOEMDRO(818,Ftmp)
	Sleep(125)
End Function

Function SafeMoveZ(Z1, F1) As Boolean 'return 1 (error) if probe tripped
    Dim Zstart
	Dim Ftmp
	Dim AbsIncF
	SafeMoveZ=True
	Zstart = GetOEMDRO(802)
	Ftmp = FeedRate()
	AbsIncF=GetOEMLED(49)
	Code "G91"
	Code "F" & F1
	Sleep(125)
	Call WaitProbeReady()	
	Code "G31 Z" & Z1
	While IsMoving()
		Sleep(100)
	Wend
	ZHit = GetVar(2002)
	Call SetLED49(AbsIncF)
	If Abs(ZHit - Zstart - Z1 -GetOEMDRO(42)) > 0.01 Then
		SafeMoveZ=False
		PushMSG("Error! Probe tripped during Z movement")
	End If 
	SetOEMDRO(818,Ftmp)
	Sleep(125)
End Function

Function PushMSG(Str1 As String) As Boolean
	SetUserLabel (21,GetUserLabel(20))
	SetUserLabel (20,GetUserLabel(19))
	SetUserLabel (19,GetUserLabel(18))
	SetUserLabel (18,GetUserLabel(17))
	SetUserLabel (17,Str1)
	Message Str1
	PushMSG=True
End Function
   
Sub WaitProbeReady()
	While GetOemLED(825)
		Sleep(100)
	Wend
End Sub

Sub SetLED49(Flag)
	If Flag Then
		Code "G91"
		Sleep(125)
	Else
		Code "G90"
		Sleep(125)
	End If
End Sub 
  
