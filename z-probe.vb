CurrentFeed = GetOemDRO(818)
DoSpinStop()

Dim ProbePuckHeight 'This is the height in mm of the probe
ProbePuckHeight = 19.04 'In this case our puck height is just over 19 mm. This means when the tool touches the probe z will be 19.04mm below the current DRO position.

Dim MaxSearchDepth 'This is how far in mm the z axis will move without hitting getting a probe result.
MaxSearchDepth = 20

ZCur = GetDro(2)
ZMove = ZCur-MaxSearchDepth 
ZOffset = 19.04 'Probe thickness in mm?
ZSal = ProbePuckHeight + 8.0 'After the completion of the probe, put on the 2.0

If GetOemLed (825)=0 Then
	Code "G4 P2.5"
	Code "G31 Z-"& ZMove & "F100"
	
	While IsMoving()
		Sleep(200)
	Wend
	
	Probepos = GetDro(2)
	Call SetDro(2, ProbePuckHeight)
	Code "G4 P1"
	Code "G90 G1 Z+" & ZSal &"F" &600
	Code "(Z zeroed)"
	Code "F" &CurrentFeed

Else
	Code "(Check Ground Probe)"
End If

Exit Sub         
    
