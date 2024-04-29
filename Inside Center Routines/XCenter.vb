Sub Main()

' X-X+ Inside Probing Script
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

Dim Zpos, Xpos

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

AbsIncFlag = GetOEMLED(49)   ' Get the current G90/G91 state

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

	'Save Z start position
	Zpos = GetOEMDRO(802)
	Xpos = GetOEMDRO(800)
	'Safe Go to X- start position
	'If Not SafeMoveX((XYclearance-EdgeLength),CurrentFeed) Then
	'	PushMSG("Manually return to the starting position and repeat the search")
	'	Exit Sub 
	'End If
	'If Not SafeMoveZ((-Zdepth),CurrentFeed) Then 
	'	PushMSG("Manually return to the starting position and repeat the search")
	'	Exit Sub 
	'End If
	'Probe X-
	XHit=ProbeX(-1,DMax,Latch,FRate1,FRate2)
	If XHit=999999 Then 
		Exit Sub 
	End If
	'Indicate result
	SetUserLabel (2, Format(XHit-ProbeD/2, "####0.000"))
	'Move to X+ start position
	If Not SafeMoveX(-1*(GetOEMDRO(800)-Xpos),FRate1) Then 
		PushMSG("Return to the search position is interrupted")
		Exit Sub 
	End If
	'Probe X+
	XbHit=ProbeX(1,DMax,Latch,FRate1,FRate2)
	If XbHit=999999 Then 
		Exit Sub 
	End If
	'Indicate result
	SetUserLabel (4, Format(XbHit+ProbeD/2, "####0.000"))
	SetUserLabel (3, Format((XbHit+XHit)/2, "####0.000"))
	SetUserLabel (5, Format(Abs(XbHit-XHit+ProbeD), "####0.000"))
	PushMSG("X+ = " & (XBHit+ProbeD/2) & ", Xc = " & (XbHit+XHit)/2 & ", X- = " & (XHit-ProbeD/2) & ", Lx = " & Abs(XbHit-XHit+ProbeD))
	'Safe back to start position
	'If Not SafeMoveZ((Zdepth),CurrentFeed) Then 
	'	PushMSG("Return to the search position is interrupted")
	'	Exit Sub 
	'End If
	'Move to Center point
	If Not SafeMoveX((XbHit+XHit)/2-GetOEMDRO(800),FRate1) Then 
		PushMSG("Return to the search position is interrupted")
		Exit Sub 
	End If


	If AutoZeroFlag = 1 Then 
		SetOEMDRO(800, GetOEMDRO(800)-(XbHit+XHit)/2) 
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

Function ProbeX(Dir,DMax,Latch,FRate1,FRate2)
	Dim Res
    	Dim Xstart
	Dim Ftmp
	Dim AbsIncF
	Dim Hit
	ProbeX=999999
	Xstart = GetOEMDRO(800)
	Ftmp = FeedRate() 'FeedRate()
	AbsIncF=GetOEMLED(49)
	'Fast Probe X
	Code "G91"
	Code "F" & FRate1
	Sleep(125)
	Call WaitProbeReady()	
	Code "G31 X" & (Dir*DMax)
	While IsMoving()
	Wend
	Res = GetVar(2000)
	Hit = GetOEMDRO(800)
'	PushMSG("Res=" & Res &", Xstart=" & Xstart & ", DMax=" & DMax & ", FRate1=" & FRate1)
	If Abs(Hit - Xstart - Dir*DMax) < 0.01 Then
		PushMSG("Error: G31 X search finished without making contact")
		PushMSG("Manually return to the starting position and repeat the search")
		Call SetLED49(AbsIncF)
		SetOEMDRO(818,Ftmp)
		Sleep(125)
		Exit Function 
	End If 
	'Move back
	Code "G01 X" & -Dir*Latch
	While IsMoving()
	Wend
	Call WaitProbeReady()	
	'Latch Probe X
	Xstart = GetDRO(0)
	Code "F" & FRate2
	Sleep(125)
	Code "G31 X" & Dir*Latch*2
	While IsMoving()
	Wend
	'Save result
	Res = GetVar(2000)
	Hit = GetOEMDRO(800)
	If Abs(Hit - Xstart - Dir*Latch*2) < 0.01 Then
		PushMSG("Error: G31 X latch finished without making contact")
		PushMSG("Manually return to the starting position and repeat the search")
		Call SetLED49(AbsIncF)
		SetOEMDRO(818,Ftmp)
		Sleep(125)
		Exit Function 
	End If 
	Code "F" & FRate1
	Code "G01 X" & -Dir*Latch
	While IsMoving()
	Wend
	Call SetLED49(AbsIncF)
	SetOEMDRO(818,Ftmp)
	Sleep(125)
	ProbeX=Hit
End Function

Function SafeMoveX(X1, F1) As Boolean 'return 1 (error) if probe tripped
    Dim Xstart
	Dim Ftmp
	Dim AbsIncF
	Dim XHit
	SafeMoveX=True
	Xstart = GetDRO(0)
	Ftmp = FeedRate() 'FeedRate()
	AbsIncF=GetOEMLED(49)
	Code "G91"
	Code "F" & F1
	Sleep(125)
	Call WaitProbeReady()	
	Code "G31 X" & X1
	While IsMoving()
	Wend
	XHit = GetVar(2000)
'	PushMSG("XHit=" & XHit &", Xstart=" & Xstart & ", X1=" & X1 & ", Ftmp=" & Ftmp)
'	PushMSG("XHit - Xstart - X1=" & XHit - Xstart - X1)
	Call SetLED49(AbsIncF)
	If Abs(XHit - Xstart - X1) > 0.01 Then
		SafeMoveX=False
		PushMSG("Error! Probe tripped during X movement")
	End If 
	SetOEMDRO(818,Ftmp)
	Sleep(125)
End Function

Function SafeMoveZ(Z1, F1) As Boolean 'return 1 (error) if probe tripped
    Dim Zstart
	Dim Ftmp
	Dim AbsIncF
	SafeMoveZ=True
	Zstart = GetDRO(2)
	Ftmp = FeedRate() 'FeedRate()
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
 
