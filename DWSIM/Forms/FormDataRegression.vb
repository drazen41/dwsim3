﻿'    Data Regression Utility
'    Copyright 2012 Daniel Wagner O. de Medeiros
'
'    This file is part of DWSIM.
'
'    DWSIM is free software: you can redistribute it and/or modify
'    it under the terms of the GNU General Public License as published by
'    the Free Software Foundation, either version 3 of the License, or
'    (at your option) any later version.
'
'    DWSIM is distributed in the hope that it will be useful,
'    but WITHOUT ANY WARRANTY; without even the implied warranty of
'    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'    GNU General Public License for more details.
'
'    You should have received a copy of the GNU General Public License
'    along with DWSIM.  If not, see <http://www.gnu.org/licenses/>.

Imports DWSIM.DWSIM.ClassesBasicasTermodinamica
Imports Microsoft.Msdn.Samples
Imports DWSIM.DWSIM.MathEx
Imports System.Math
Imports ZedGraph
Imports DotNumerics
Imports Cureos.Numerics
Imports DWSIM.DWSIM.Optimization.DatRegression
Imports System.Threading.Tasks

Public Class FormDataRegression

    Public cv As DWSIM.SistemasDeUnidades.Conversor
    Public fmin As Double
    Public info As Integer
    Public cancel As Boolean = False

    Public finalval2() As Double = Nothing
    Public itn As Integer = 0

    Private _penval As Double = 0
    Private forceclose As Boolean = False

    Public currcase As RegressionCase

    Public proppack As DWSIM.SimulationObjects.PropertyPackages.PropertyPackage
    Public ppname As String = ""

    Private Sub FormDataRegression_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        cv = New DWSIM.SistemasDeUnidades.Conversor

        'get list of compounds
        Dim compounds As New ArrayList
        For Each c As ConstantProperties In FormMain.AvailableComponents.Values
            'compounds.Add(DWSIM.App.GetComponentName(c.Name))
            compounds.Add(c.Name)
        Next

        compounds.Sort()

        Me.cbCompound1.Items.AddRange(compounds.ToArray())
        Me.cbCompound2.Items.AddRange(compounds.ToArray())

        LoadCase(New RegressionCase, True)

    End Sub

    Private Sub FormDataRegression_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing

        If Not forceclose Then
            Dim x = MessageBox.Show(DWSIM.App.GetLocalString("Desejasalvarasaltera"), DWSIM.App.GetLocalString("Fechando") & " " & Me.Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question)

            If x = MsgBoxResult.Yes Then

                Call FormMain.SaveFileDialog()
                forceclose = True
                Me.Close()

            ElseIf x = MsgBoxResult.Cancel Then

                e.Cancel = True

            Else

                forceclose = True
                Me.Close()

            End If
        End If

    End Sub

    Function StoreCase() As RegressionCase

        Dim mycase As New RegressionCase

        With mycase
            .comp1 = Me.cbCompound1.SelectedItem.ToString
            .comp2 = Me.cbCompound2.SelectedItem.ToString
            .model = Me.cbModel.SelectedItem.ToString
            .includesd = Me.chkIncludeSD.Checked
            .datatype = Me.cbDataType.SelectedIndex
            .method = Me.cbRegMethod.SelectedItem.ToString
            .objfunction = Me.cbObjFunc.SelectedItem.ToString
            .tunit = Me.cbTunit.SelectedItem.ToString
            .punit = Me.cbPunit.SelectedItem.ToString
            .title = tbTitle.Text
            .description = tbDescription.Text
            .results = tbRegResults.Text
            For Each r As DataGridViewRow In Me.GridExpData.Rows
                If r.Index < Me.GridExpData.Rows.Count - 1 Then
                    If Double.TryParse(r.Cells("colx1").Value, New Double) Then .x1p.Add(Double.Parse(r.Cells("colx1").Value)) Else .x1p.Add(0.0#)
                    If Double.TryParse(r.Cells("colx2").Value, New Double) Then .x2p.Add(Double.Parse(r.Cells("colx2").Value)) Else .x2p.Add(0.0#)
                    If Double.TryParse(r.Cells("coly1").Value, New Double) Then .yp.Add(Double.Parse(r.Cells("coly1").Value)) Else .yp.Add(0.0#)
                    If Double.TryParse(r.Cells("colt").Value, New Double) Then .tp.Add(Double.Parse(r.Cells("colt").Value)) Else .tp.Add(0.0#)
                    If Double.TryParse(r.Cells("colp").Value, New Double) Then .pp.Add(Double.Parse(r.Cells("colp").Value)) Else .pp.Add(0.0#)
                End If
            Next
            Select Case cbModel.SelectedItem.ToString()
                Case "Peng-Robinson", "Soave-Redlich-Kwong"
                    .iepar1 = gridInEst.Rows(0).Cells(1).Value
                Case "PC-SAFT", "Lee-Kesler-Plöcker"
                    .iepar1 = gridInEst.Rows(0).Cells(1).Value
                Case "UNIQUAC", "PRSV2-M", "PRSV2-VL"
                    .iepar1 = gridInEst.Rows(0).Cells(1).Value
                    .iepar2 = gridInEst.Rows(1).Cells(1).Value
                Case "NRTL"
                    .iepar1 = gridInEst.Rows(0).Cells(1).Value
                    .iepar2 = gridInEst.Rows(1).Cells(1).Value
                    .iepar3 = gridInEst.Rows(2).Cells(1).Value
            End Select
        End With

        Return mycase

    End Function

    Sub LoadCase(ByVal mycase As RegressionCase, ByVal first As Boolean)

        With mycase
            Me.cbCompound1.SelectedItem = .comp1
            Me.cbCompound2.SelectedItem = .comp2
            If .model.ToLower = "PRSV2" Then .model = "PRSV2-M"
            Me.cbModel.SelectedItem = .model
            Select Case cbModel.SelectedItem.ToString()
                Case "Peng-Robinson", "Soave-Redlich-Kwong"
                    gridInEst.Rows.Clear()
                    gridInEst.Rows.Add(New Object() {"kij", .iepar1})
                Case "PC-SAFT", "Lee-Kesler-Plöcker"
                    gridInEst.Rows.Clear()
                    gridInEst.Rows.Add(New Object() {"kij", .iepar1})
                Case "UNIQUAC"
                    gridInEst.Rows.Clear()
                    gridInEst.Rows.Add(New Object() {"A12", .iepar1})
                    gridInEst.Rows.Add(New Object() {"A21", .iepar2})
                Case "PRSV2-M", "PRSV2-VL"
                    gridInEst.Rows.Clear()
                    gridInEst.Rows.Add(New Object() {"kij", .iepar1})
                    gridInEst.Rows.Add(New Object() {"kji", .iepar2})
                Case "NRTL"
                    gridInEst.Rows.Clear()
                    gridInEst.Rows.Add(New Object() {"A12", .iepar1})
                    gridInEst.Rows.Add(New Object() {"A21", .iepar2})
                    gridInEst.Rows.Add(New Object() {"alpha12", .iepar3})
            End Select
            Me.chkIncludeSD.Checked = .includesd
            Me.cbDataType.SelectedIndex = .datatype
            Me.cbRegMethod.SelectedItem = .method
            Me.cbObjFunc.SelectedItem = .objfunction
            Me.cbTunit.SelectedItem = .tunit
            Me.cbPunit.SelectedItem = .punit
            Me.tbTitle.Text = .title
            Me.tbDescription.Text = .description
            Dim val1, val2, val3, val4, val5 As String, i As Integer
            For i = 0 To .x1p.Count - 1
                If Double.TryParse(.x1p(i), New Double) Then val1 = Double.Parse(.x1p(i)).ToString() Else val1 = ""
                If Double.TryParse(.x2p(i), New Double) Then val2 = Double.Parse(.x2p(i)).ToString() Else val2 = ""
                If Double.TryParse(.yp(i), New Double) Then val3 = Double.Parse(.yp(i)).ToString() Else val3 = ""
                If Double.TryParse(.tp(i), New Double) Then val4 = Double.Parse(.tp(i)).ToString() Else val4 = ""
                If Double.TryParse(.pp(i), New Double) Then val5 = Double.Parse(.pp(i)).ToString() Else val5 = ""
                Me.GridExpData.Rows.Add(val1, val2, val3, val4, val5)
            Next
            Me.tbRegResults.Text = .results
            currcase = mycase
            If Not first Then UpdateData()
        End With

    End Sub

    Private Sub cbDataType_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cbDataType.SelectedIndexChanged
        Select Case cbDataType.SelectedIndex
            Case 0
                Me.GridExpData.Columns("colx1").Visible = True
                Me.GridExpData.Columns("colx2").Visible = False
                Me.GridExpData.Columns("coly1").Visible = True
                Me.GridExpData.Columns("colT").Visible = True
                Me.GridExpData.Columns("colP").Visible = True
                cbObjFunc.Enabled = True
                Me.FaTabStripItem2.Visible = True
            Case 1
                Me.GridExpData.Columns("colx1").Visible = True
                Me.GridExpData.Columns("colx2").Visible = False
                Me.GridExpData.Columns("coly1").Visible = True
                Me.GridExpData.Columns("colT").Visible = True
                Me.GridExpData.Columns("colP").Visible = True
                cbObjFunc.Enabled = True
                Me.FaTabStripItem2.Visible = True
            Case 2
                Me.GridExpData.Columns("colx1").Visible = True
                Me.GridExpData.Columns("colx2").Visible = False
                Me.GridExpData.Columns("coly1").Visible = True
                Me.GridExpData.Columns("colT").Visible = True
                Me.GridExpData.Columns("colP").Visible = True
                cbObjFunc.Enabled = True
                Me.FaTabStripItem2.Visible = True
            Case 3
                Me.GridExpData.Columns("colx1").Visible = True
                Me.GridExpData.Columns("colx2").Visible = True
                Me.GridExpData.Columns("coly1").Visible = False
                Me.GridExpData.Columns("colT").Visible = True
                Me.GridExpData.Columns("colP").Visible = True
                cbObjFunc.Enabled = True
                Me.FaTabStripItem2.Visible = False
            Case 4
                Me.GridExpData.Columns("colx1").Visible = True
                Me.GridExpData.Columns("colx2").Visible = True
                Me.GridExpData.Columns("coly1").Visible = False
                Me.GridExpData.Columns("colT").Visible = True
                Me.GridExpData.Columns("colP").Visible = True
                cbObjFunc.Enabled = True
                Me.FaTabStripItem2.Visible = False
            Case 5
                Me.GridExpData.Columns("colx1").Visible = True
                Me.GridExpData.Columns("colx2").Visible = True
                Me.GridExpData.Columns("coly1").Visible = False
                Me.GridExpData.Columns("colT").Visible = True
                Me.GridExpData.Columns("colP").Visible = True
                cbObjFunc.Enabled = True
                Me.FaTabStripItem2.Visible = False
        End Select
    End Sub

    Private Function FunctionValue(ByVal x() As Double) As Double

        Application.DoEvents()
        If cancel Then Exit Function

        Dim doparallel As Boolean = My.Settings.EnableParallelProcessing
        Dim poptions As New ParallelOptions() With {.MaxDegreeOfParallelism = My.Settings.MaxDegreeOfParallelism}

        Dim Vx1(currcase.pp.Count - 1), Vx2(currcase.pp.Count - 1), Vy(currcase.pp.Count - 1), IP(x.Length - 1, x.Length) As Double
        Dim Vx1c(currcase.pp.Count - 1), Vx2c(currcase.pp.Count - 1), Vyc(currcase.pp.Count - 1) As Double
        Dim VP(currcase.pp.Count - 1), VT(currcase.tp.Count - 1) As Double
        Dim VPc(currcase.pp.Count - 1), VTc(currcase.tp.Count - 1) As Double
        Dim np As Integer = currcase.x1p.Count
        Dim i As Integer = 0
        Dim PVF As Boolean = False

        Select Case currcase.datatype
            Case DataType.Pxy
                For i = 0 To np - 1
                    Vx1(i) = currcase.x1p(i)
                    Vy(i) = currcase.yp(i)
                    VP(i) = cv.ConverterParaSI(currcase.punit, currcase.pp(i))
                    VT(i) = cv.ConverterParaSI(currcase.tunit, currcase.tp(0))
                Next
            Case DataType.Txy
                For i = 0 To np - 1
                    Vx1(i) = currcase.x1p(i)
                    Vy(i) = currcase.yp(i)
                    VP(i) = cv.ConverterParaSI(currcase.punit, currcase.pp(0))
                    VT(i) = cv.ConverterParaSI(currcase.tunit, currcase.tp(i))
                Next
                PVF = True
            Case DataType.TPxy
                For i = 0 To np - 1
                    Vx1(i) = currcase.x1p(i)
                    Vy(i) = currcase.yp(i)
                    VP(i) = cv.ConverterParaSI(currcase.punit, currcase.pp(i))
                    VT(i) = cv.ConverterParaSI(currcase.tunit, currcase.tp(i))
                Next
            Case DataType.Pxx
                For i = 0 To np - 1
                    Vx1(i) = currcase.x1p(i)
                    Vx2(i) = currcase.x2p(i)
                    VP(i) = cv.ConverterParaSI(currcase.punit, currcase.pp(i))
                    VT(i) = cv.ConverterParaSI(currcase.tunit, currcase.tp(0))
                Next
            Case DataType.Txx
                For i = 0 To np - 1
                    Vx1(i) = currcase.x1p(i)
                    Vx2(i) = currcase.x2p(i)
                    VP(i) = cv.ConverterParaSI(currcase.punit, currcase.pp(0))
                    VT(i) = cv.ConverterParaSI(currcase.tunit, currcase.tp(i))
                Next
            Case DataType.TPxx
                For i = 0 To np - 1
                    Vx1(i) = currcase.x1p(i)
                    Vx2(i) = currcase.x2p(i)
                    VP(i) = cv.ConverterParaSI(currcase.punit, currcase.pp(i))
                    VT(i) = cv.ConverterParaSI(currcase.tunit, currcase.tp(i))
                Next
        End Select

        Dim f As Double = 0.0#
        Dim result As Object = Nothing
        Dim vartext As String = ""

        Try

            Me.currcase.calcp.Clear()
            Me.currcase.calct.Clear()
            Me.currcase.calcy.Clear()
            Me.currcase.calcx1l1.Clear()
            Me.currcase.calcx1l2.Clear()

            Select Case currcase.datatype
                Case DataType.Pxy, DataType.Txy
                    Select Case currcase.model
                        Case "PC-SAFT", "Peng-Robinson", "Soave-Redlich-Kwong", "Lee-Kesler-Plöcker"
                            If PVF Then
                                If doparallel Then
                                    My.Application.IsRunningParallelTasks = True
                                    proppack.FlashAlgorithm = DWSIM.SimulationObjects.PropertyPackages.FlashMethod.DWSIMDefault
                                    Interfaces.ExcelIntegration.AddCompounds(proppack, New Object() {currcase.comp1, currcase.comp2})
                                    Interfaces.ExcelIntegration.SetIP(proppack.ComponentName, proppack, New Object() {currcase.comp1, currcase.comp2}, New Double(,) {{0.0#, x(0)}, {x(0), 0.0#}}, Nothing, Nothing, Nothing)
                                    Try
                                        Dim task1 As Task = Task.Factory.StartNew(Sub() Parallel.For(0, np, poptions,
                                                                                 Sub(ipar)
                                                                                     Dim result2 As Object
                                                                                     result2 = proppack.DW_CalcBubT(New Double() {Vx1(ipar), 1 - Vx1(ipar)}, VP(0), VT(ipar))
                                                                                     VTc(ipar) = result2(4)
                                                                                     Vyc(ipar) = result2(3)(0)
                                                                                 End Sub))
                                        task1.Wait()
                                    Catch ae As AggregateException
                                        For Each ex As Exception In ae.InnerExceptions
                                            Throw ex
                                        Next
                                    End Try
                                    My.Application.IsRunningParallelTasks = False
                                Else
                                    For i = 0 To np - 1
                                        result = Interfaces.ExcelIntegration.PVFFlash(proppack, 1, VP(0), 0.0#, New Object() {currcase.comp1, currcase.comp2}, New Double() {Vx1(i), 1 - Vx1(i)}, New Double(,) {{0.0#, x(0)}, {x(0), 0.0#}}, Nothing, Nothing, Nothing)
                                        VTc(i) = result(4, 0)
                                        Vyc(i) = result(2, 0)
                                    Next
                                End If
                            Else
                                If doparallel Then
                                    My.Application.IsRunningParallelTasks = True
                                    proppack.FlashAlgorithm = DWSIM.SimulationObjects.PropertyPackages.FlashMethod.DWSIMDefault
                                    Interfaces.ExcelIntegration.AddCompounds(proppack, New Object() {currcase.comp1, currcase.comp2})
                                    Interfaces.ExcelIntegration.SetIP(proppack.ComponentName, proppack, New Object() {currcase.comp1, currcase.comp2}, New Double(,) {{0.0#, x(0)}, {x(0), 0.0#}}, Nothing, Nothing, Nothing)
                                    Try
                                        Dim task1 As Task = Task.Factory.StartNew(Sub() Parallel.For(0, np, poptions,
                                                                                 Sub(ipar)
                                                                                     Dim result2 As Object
                                                                                     result2 = proppack.DW_CalcBubP(New Double() {Vx1(ipar), 1 - Vx1(ipar)}, VT(0), VP(ipar))
                                                                                     VPc(ipar) = result2(4)
                                                                                     Vyc(ipar) = result2(3)(0)
                                                                                 End Sub))
                                        task1.Wait()
                                    Catch ae As AggregateException
                                        For Each ex As Exception In ae.InnerExceptions
                                            Throw ex
                                        Next
                                    End Try
                                    My.Application.IsRunningParallelTasks = False
                                Else
                                    For i = 0 To np - 1
                                        result = Interfaces.ExcelIntegration.TVFFlash(proppack, 1, VT(0), 0.0#, New Object() {currcase.comp1, currcase.comp2}, New Double() {Vx1(i), 1 - Vx1(i)}, New Double(,) {{0.0#, x(0)}, {x(0), 0.0#}}, Nothing, Nothing, Nothing)
                                        VPc(i) = result(4, 0)
                                        Vyc(i) = result(2, 0)
                                    Next
                                End If
                            End If
                            vartext = ", Interaction parameters = {"
                            vartext += "kij = " & x(0).ToString("N4")
                            vartext += "}"
                        Case "UNIQUAC"
                            If PVF Then
                                If doparallel Then
                                    My.Application.IsRunningParallelTasks = True
                                    proppack.FlashAlgorithm = DWSIM.SimulationObjects.PropertyPackages.FlashMethod.DWSIMDefault
                                    Interfaces.ExcelIntegration.AddCompounds(proppack, New Object() {currcase.comp1, currcase.comp2})
                                    Interfaces.ExcelIntegration.SetIP(proppack.ComponentName, proppack, New Object() {currcase.comp1, currcase.comp2}, New Double(,) {{0.0#, 0.0#}, {0.0#, 0.0#}}, New Double(,) {{0.0#, x(0)}, {x(1), 0.0#}}, New Double(,) {{0.0#, x(1)}, {x(0), 0.0#}}, Nothing)
                                    Try
                                        Dim task1 As Task = Task.Factory.StartNew(Sub() Parallel.For(0, np, poptions,
                                                                                 Sub(ipar)
                                                                                     Dim result2 As Object
                                                                                     result2 = proppack.DW_CalcBubT(New Double() {Vx1(ipar), 1 - Vx1(ipar)}, VP(0), VT(ipar))
                                                                                     VTc(ipar) = result2(4)
                                                                                     Vyc(ipar) = result2(3)(0)
                                                                                 End Sub))
                                        task1.Wait()
                                    Catch ae As AggregateException
                                        For Each ex As Exception In ae.InnerExceptions
                                            Throw ex
                                        Next
                                    End Try
                                    My.Application.IsRunningParallelTasks = False
                                Else
                                    For i = 0 To np - 1
                                        result = Interfaces.ExcelIntegration.PVFFlash(proppack, 1, VP(0), 0.0#, New Object() {currcase.comp1, currcase.comp2}, New Double() {Vx1(i), 1 - Vx1(i)}, New Double(,) {{0.0#, x(0)}, {x(0), 0.0#}}, Nothing, Nothing, Nothing)
                                        VTc(i) = result(4, 0)
                                        Vyc(i) = result(2, 0)
                                    Next
                                End If
                            Else
                                If doparallel Then
                                    My.Application.IsRunningParallelTasks = True
                                    proppack.FlashAlgorithm = DWSIM.SimulationObjects.PropertyPackages.FlashMethod.DWSIMDefault
                                    Interfaces.ExcelIntegration.AddCompounds(proppack, New Object() {currcase.comp1, currcase.comp2})
                                    Interfaces.ExcelIntegration.SetIP(proppack.ComponentName, proppack, New Object() {currcase.comp1, currcase.comp2}, New Double(,) {{0.0#, 0.0#}, {0.0#, 0.0#}}, New Double(,) {{0.0#, x(0)}, {x(1), 0.0#}}, New Double(,) {{0.0#, x(1)}, {x(0), 0.0#}}, Nothing)
                                    Try
                                        Dim task1 As Task = Task.Factory.StartNew(Sub() Parallel.For(0, np, poptions,
                                                                             Sub(ipar)
                                                                                 Dim result2 As Object
                                                                                 result2 = proppack.DW_CalcBubP(New Double() {Vx1(ipar), 1 - Vx1(ipar)}, VT(0), VP(ipar))
                                                                                 VPc(ipar) = result2(4)
                                                                                 Vyc(ipar) = result2(3)(0)
                                                                             End Sub))
                                        task1.Wait()
                                    Catch ae As AggregateException
                                        For Each ex As Exception In ae.InnerExceptions
                                            Throw ex
                                        Next
                                    End Try
                                    My.Application.IsRunningParallelTasks = False
                                Else
                                    For i = 0 To np - 1
                                        result = Interfaces.ExcelIntegration.TVFFlash(proppack, 1, VT(0), 0.0#, New Object() {currcase.comp1, currcase.comp2}, New Double() {Vx1(i), 1 - Vx1(i)}, New Double(,) {{0.0#, x(0)}, {x(0), 0.0#}}, Nothing, Nothing, Nothing)
                                        VPc(i) = result(4, 0)
                                        Vyc(i) = result(2, 0)
                                    Next
                                End If
                            End If
                            vartext = ", Interaction parameters = {"
                            vartext += "A12 = " & x(0).ToString("N4") & ", "
                            vartext += "A21 = " & x(1).ToString("N4")
                            vartext += "}"
                        Case "PRSV2-M", "PRSV2-VL"
                            If PVF Then
                                If doparallel Then
                                    My.Application.IsRunningParallelTasks = True
                                    proppack.FlashAlgorithm = DWSIM.SimulationObjects.PropertyPackages.FlashMethod.DWSIMDefault
                                    Interfaces.ExcelIntegration.AddCompounds(proppack, New Object() {currcase.comp1, currcase.comp2})
                                    Interfaces.ExcelIntegration.SetIP(proppack.ComponentName, proppack, New Object() {currcase.comp1, currcase.comp2}, New Double(,) {{0.0#, x(0)}, {x(0), 0.0#}}, New Double(,) {{0.0#, x(1)}, {x(1), 0.0#}}, Nothing, Nothing)
                                    Try
                                        Dim task1 As Task = Task.Factory.StartNew(Sub() Parallel.For(0, np, poptions,
                                                                                 Sub(ipar)
                                                                                     Dim result2 As Object
                                                                                     result2 = proppack.DW_CalcBubT(New Double() {Vx1(ipar), 1 - Vx1(ipar)}, VP(0), VT(ipar))
                                                                                     VTc(ipar) = result2(4)
                                                                                     Vyc(ipar) = result2(3)(0)
                                                                                 End Sub))
                                        task1.Wait()
                                    Catch ae As AggregateException
                                        For Each ex As Exception In ae.InnerExceptions
                                            Throw ex
                                        Next
                                    End Try
                                    My.Application.IsRunningParallelTasks = False
                                Else
                                    For i = 0 To np - 1
                                        result = Interfaces.ExcelIntegration.PVFFlash(proppack, 1, VP(0), 0.0#, New Object() {currcase.comp1, currcase.comp2}, New Double() {Vx1(i), 1 - Vx1(i)}, New Double(,) {{0.0#, x(0)}, {x(0), 0.0#}}, New Double(,) {{0.0#, x(1)}, {x(1), 0.0#}}, Nothing, Nothing)
                                        VTc(i) = result(4, 0)
                                        Vyc(i) = result(2, 0)
                                    Next
                                End If
                            Else
                                If doparallel Then
                                    My.Application.IsRunningParallelTasks = True
                                    proppack.FlashAlgorithm = DWSIM.SimulationObjects.PropertyPackages.FlashMethod.DWSIMDefault
                                    Interfaces.ExcelIntegration.AddCompounds(proppack, New Object() {currcase.comp1, currcase.comp2})
                                    Interfaces.ExcelIntegration.SetIP(proppack.ComponentName, proppack, New Object() {currcase.comp1, currcase.comp2}, New Double(,) {{0.0#, x(0)}, {x(0), 0.0#}}, New Double(,) {{0.0#, x(1)}, {x(1), 0.0#}}, Nothing, Nothing)
                                    Try
                                        Dim task1 As Task = Task.Factory.StartNew(Sub() Parallel.For(0, np, poptions,
                                                                                 Sub(ipar)
                                                                                     Dim result2 As Object
                                                                                     result2 = proppack.DW_CalcBubP(New Double() {Vx1(ipar), 1 - Vx1(ipar)}, VT(0), VP(ipar))
                                                                                     VPc(ipar) = result2(4)
                                                                                     Vyc(ipar) = result2(3)(0)
                                                                                 End Sub))
                                        task1.Wait()
                                    Catch ae As AggregateException
                                        For Each ex As Exception In ae.InnerExceptions
                                            Throw ex
                                        Next
                                    End Try
                                    My.Application.IsRunningParallelTasks = False
                                Else
                                    For i = 0 To np - 1
                                        result = Interfaces.ExcelIntegration.TVFFlash(proppack, 1, VT(0), 0.0#, New Object() {currcase.comp1, currcase.comp2}, New Double() {Vx1(i), 1 - Vx1(i)}, New Double(,) {{0.0#, x(0)}, {x(0), 0.0#}}, Nothing, Nothing, Nothing)
                                        VPc(i) = result(4, 0)
                                        Vyc(i) = result(2, 0)
                                    Next
                                End If
                            End If
                            vartext = ", Interaction parameters = {"
                            vartext += "kij = " & x(0).ToString("N4") & ", "
                            vartext += "kji = " & x(1).ToString("N4")
                            vartext += "}"
                        Case "NRTL"
                            If PVF Then
                                If doparallel Then
                                    My.Application.IsRunningParallelTasks = True
                                    proppack.FlashAlgorithm = DWSIM.SimulationObjects.PropertyPackages.FlashMethod.DWSIMDefault
                                    Interfaces.ExcelIntegration.AddCompounds(proppack, New Object() {currcase.comp1, currcase.comp2})
                                    Interfaces.ExcelIntegration.SetIP(proppack.ComponentName, proppack, New Object() {currcase.comp1, currcase.comp2}, New Double(,) {{0.0#, 0.0#}, {0.0#, 0.0#}}, New Double(,) {{0.0#, x(0)}, {x(1), 0.0#}}, New Double(,) {{0.0#, x(1)}, {x(0), 0.0#}}, New Double(,) {{0.0#, x(2)}, {x(2), 0.0#}})
                                    Try
                                        Dim task1 As Task = Task.Factory.StartNew(Sub() Parallel.For(0, np, poptions,
                                                                                            Sub(ipar)
                                                                                                Dim result2 As Object
                                                                                                result2 = proppack.DW_CalcBubT(New Double() {Vx1(ipar), 1 - Vx1(ipar)}, VP(0), VT(ipar))
                                                                                                VTc(ipar) = result2(4)
                                                                                                Vyc(ipar) = result2(3)(0)
                                                                                            End Sub))
                                        task1.Wait()
                                    Catch ae As AggregateException
                                        For Each ex As Exception In ae.InnerExceptions
                                            Throw ex
                                        Next
                                    End Try
                                    My.Application.IsRunningParallelTasks = False
                                Else
                                    For i = 0 To np - 1
                                        result = Interfaces.ExcelIntegration.PVFFlash(proppack, 1, VP(0), 0.0#, New Object() {currcase.comp1, currcase.comp2}, New Double() {Vx1(i), 1 - Vx1(i)}, New Double(,) {{0.0#, 0.0#}, {0.0#, 0.0#}}, New Double(,) {{0.0#, x(0)}, {x(1), 0.0#}}, New Double(,) {{0.0#, x(1)}, {x(0), 0.0#}}, New Double(,) {{0.0#, x(2)}, {x(2), 0.0#}})
                                        VTc(i) = result(4, 0)
                                        Vyc(i) = result(2, 0)
                                    Next
                                End If
                            Else
                                If doparallel Then
                                    My.Application.IsRunningParallelTasks = True
                                    proppack.FlashAlgorithm = DWSIM.SimulationObjects.PropertyPackages.FlashMethod.DWSIMDefault
                                    Interfaces.ExcelIntegration.AddCompounds(proppack, New Object() {currcase.comp1, currcase.comp2})
                                    Interfaces.ExcelIntegration.SetIP(proppack.ComponentName, proppack, New Object() {currcase.comp1, currcase.comp2}, New Double(,) {{0.0#, 0.0#}, {0.0#, 0.0#}}, New Double(,) {{0.0#, x(0)}, {x(1), 0.0#}}, New Double(,) {{0.0#, x(1)}, {x(0), 0.0#}}, New Double(,) {{0.0#, x(2)}, {x(2), 0.0#}})
                                    Try
                                        Dim task1 As Task = Task.Factory.StartNew(Sub() Parallel.For(0, np, poptions,
                                                                                 Sub(ipar)
                                                                                     Dim result2 As Object
                                                                                     result2 = proppack.DW_CalcBubP(New Double() {Vx1(ipar), 1 - Vx1(ipar)}, VT(0), VP(ipar))
                                                                                     VPc(ipar) = result2(4)
                                                                                     Vyc(ipar) = result2(3)(0)
                                                                                 End Sub))
                                        task1.Wait()
                                    Catch ae As AggregateException
                                        For Each ex As Exception In ae.InnerExceptions
                                            Throw ex
                                        Next
                                    End Try
                                    My.Application.IsRunningParallelTasks = False
                                Else
                                    For i = 0 To np - 1
                                        result = Interfaces.ExcelIntegration.TVFFlash(proppack, 1, VT(0), 0.0#, New Object() {currcase.comp1, currcase.comp2}, New Double() {Vx1(i), 1 - Vx1(i)}, New Double(,) {{0.0#, 0.0#}, {0.0#, 0.0#}}, New Double(,) {{0.0#, x(0)}, {x(1), 0.0#}}, New Double(,) {{0.0#, x(1)}, {x(0), 0.0#}}, New Double(,) {{0.0#, x(2)}, {x(2), 0.0#}})
                                        VPc(i) = result(4, 0)
                                        Vyc(i) = result(2, 0)
                                    Next
                                End If
                            End If
                            vartext = ", Interaction parameters = {"
                            vartext += "A12 = " & x(0).ToString("N4") & ", "
                            vartext += "A21 = " & x(1).ToString("N4") & ", "
                            vartext += "alpha12 = " & x(2).ToString("N4")
                            vartext += "}"
                    End Select
                    For i = 0 To np - 1
                        Me.currcase.calct.Add(VTc(i))
                        Me.currcase.calcp.Add(VPc(i))
                        Me.currcase.calcy.Add(Vyc(i))
                        Me.currcase.calcx1l1.Add(0.0#)
                        Me.currcase.calcx1l2.Add(0.0#)
                        Select Case currcase.objfunction
                            Case "Least Squares (min T/P+y/x)"
                                If PVF Then
                                    f += (VTc(i) - VT(i)) ^ 2 + ((Vyc(i) - Vy(i))) ^ 2
                                Else
                                    f += (VTc(i) - VP(i)) ^ 2 + ((Vyc(i) - Vy(i))) ^ 2
                                End If
                            Case "Least Squares (min T/P)"
                                If PVF Then
                                    f += (VTc(i) - VT(i)) ^ 2
                                Else
                                    f += (VPc(i) - VP(i)) ^ 2
                                End If
                            Case "Least Squares (min y/x)"
                                If PVF Then
                                    f += ((Vyc(i) - Vy(i))) ^ 2
                                Else
                                    f += ((Vyc(i) - Vy(i))) ^ 2
                                End If
                            Case "Weighted Least Squares (min T/P+y/x)"
                                If PVF Then
                                    f += ((VTc(i) - VT(i)) / VT(i)) ^ 2 + (((Vyc(i) - Vy(i))) / Vy(i)) ^ 2
                                Else
                                    f += ((VPc(i) - VP(i)) / VP(i)) ^ 2 + (((Vyc(i) - Vy(i))) / Vy(i)) ^ 2
                                End If
                            Case "Weighted Least Squares (min T/P)"
                                If PVF Then
                                    f += ((VTc(i) - VT(i)) / VT(i)) ^ 2
                                Else
                                    f += ((VPc(i) - VP(i)) / VP(i)) ^ 2
                                End If
                            Case "Weighted Least Squares (min y/x)"
                                If PVF Then
                                    f += ((Vyc(i) - Vy(i)) / Vy(i)) ^ 2
                                Else
                                    f += ((Vyc(i) - Vy(i)) / Vy(i)) ^ 2
                                End If
                            Case "Chi Square"
                        End Select
                    Next
                Case DataType.TPxy
                Case DataType.Pxx, DataType.Txx
                    Select Case currcase.model
                        Case "PRSV2-M", "PRSV2-VL"
                            If doparallel Then
                                My.Application.IsRunningParallelTasks = True
                                proppack.Parameters("PP_FLASHALGORITHM") = 3
                                proppack.FlashAlgorithm = DWSIM.SimulationObjects.PropertyPackages.FlashMethod.InsideOut3P
                                proppack._tpcompids = New String() {currcase.comp1, currcase.comp2}
                                proppack._tpseverity = 0
                                Interfaces.ExcelIntegration.AddCompounds(proppack, New Object() {currcase.comp1, currcase.comp2})
                                Interfaces.ExcelIntegration.SetIP(proppack.ComponentName, proppack, New Object() {currcase.comp1, currcase.comp2}, New Double(,) {{0.0#, x(0)}, {x(0), 0.0#}}, New Double(,) {{0.0#, x(1)}, {x(1), 0.0#}}, Nothing, Nothing)
                                Try
                                    Dim task1 As Task = Task.Factory.StartNew(Sub() Parallel.For(0, np, poptions,
                                                                             Sub(ipar)
                                                                                 Dim result2 As Object
                                                                                 result2 = proppack.FlashBase.Flash_PT(New Double() {0.5, 0.5}, VP(0), VT(ipar), proppack)
                                                                                 Vx1c(ipar) = result(2)(0)
                                                                                 Vx2c(ipar) = result(6)(0)
                                                                             End Sub))
                                    task1.Wait()
                                Catch ae As AggregateException
                                    For Each ex As Exception In ae.InnerExceptions
                                        Throw ex
                                    Next
                                End Try
                                My.Application.IsRunningParallelTasks = False
                            Else
                                proppack.Parameters("PP_FLASHALGORITHM") = 5
                                proppack.FlashAlgorithm = DWSIM.SimulationObjects.PropertyPackages.FlashMethod.GibbsMin3P
                                proppack._tpcompids = New String() {currcase.comp1, currcase.comp2}
                                proppack._tpseverity = 0
                                Interfaces.ExcelIntegration.AddCompounds(proppack, New Object() {currcase.comp1, currcase.comp2})
                                Interfaces.ExcelIntegration.SetIP(proppack.ComponentName, proppack, New Object() {currcase.comp1, currcase.comp2}, New Double(,) {{0.0#, x(0)}, {x(0), 0.0#}}, New Double(,) {{0.0#, x(1)}, {x(1), 0.0#}}, Nothing, Nothing)
                                For i = 0 To np - 1
                                    result = proppack.FlashBase.Flash_PT(New Double() {0.5, 0.5}, VP(0), VT(i), proppack)
                                    Vx1c(i) = result(2)(0)
                                    Vx2c(i) = result(6)(0)
                                Next
                            End If
                            vartext = ", Interaction parameters = {"
                            vartext += "kij = " & x(0).ToString("N4") & ", "
                            vartext += "kji = " & x(1).ToString("N4")
                            vartext += "}"
                        Case "UNIQUAC"
                            If doparallel Then
                                My.Application.IsRunningParallelTasks = True
                                proppack.Parameters("PP_FLASHALGORITHM") = 3
                                proppack.FlashAlgorithm = DWSIM.SimulationObjects.PropertyPackages.FlashMethod.InsideOut3P
                                proppack._tpcompids = New String() {currcase.comp1, currcase.comp2}
                                proppack._tpseverity = 0
                                Interfaces.ExcelIntegration.AddCompounds(proppack, New Object() {currcase.comp1, currcase.comp2})
                                Interfaces.ExcelIntegration.SetIP(proppack.ComponentName, proppack, New Object() {currcase.comp1, currcase.comp2}, New Double(,) {{0.0#, 0.0#}, {0.0#, 0.0#}}, New Double(,) {{0.0#, x(0)}, {x(1), 0.0#}}, New Double(,) {{0.0#, x(1)}, {x(0), 0.0#}}, Nothing)
                                Try
                                    Dim task1 As Task = Task.Factory.StartNew(Sub() Parallel.For(0, np, poptions,
                                                                             Sub(ipar)
                                                                                 Dim result2 As Object
                                                                                 result2 = proppack.FlashBase.Flash_PT(New Double() {0.5, 0.5}, VP(0), VT(ipar), proppack)
                                                                                 Vx1c(ipar) = result2(2)(0)
                                                                                 Vx2c(ipar) = result2(6)(0)
                                                                             End Sub))
                                    task1.Wait()
                                Catch ae As AggregateException
                                    For Each ex As Exception In ae.InnerExceptions
                                        Throw ex
                                    Next
                                End Try
                                My.Application.IsRunningParallelTasks = False
                                Application.DoEvents()
                            Else
                                proppack.Parameters("PP_FLASHALGORITHM") = 5
                                proppack.FlashAlgorithm = DWSIM.SimulationObjects.PropertyPackages.FlashMethod.GibbsMin3P
                                proppack._tpcompids = New String() {currcase.comp1, currcase.comp2}
                                proppack._tpseverity = 0
                                Interfaces.ExcelIntegration.AddCompounds(proppack, New Object() {currcase.comp1, currcase.comp2})
                                Interfaces.ExcelIntegration.SetIP(proppack.ComponentName, proppack, New Object() {currcase.comp1, currcase.comp2}, New Double(,) {{0.0#, 0.0#}, {0.0#, 0.0#}}, New Double(,) {{0.0#, x(0)}, {x(1), 0.0#}}, New Double(,) {{0.0#, x(1)}, {x(0), 0.0#}}, Nothing)
                                For i = 0 To np - 1
                                    result = proppack.FlashBase.Flash_PT(New Double() {0.5, 0.5}, VP(0), VT(i), proppack)
                                    Vx1c(i) = result(2)(0)
                                    Vx2c(i) = result(6)(0)
                                Next
                            End If
                            vartext = ", Interaction parameters = {"
                            vartext += "A12 = " & x(0).ToString("N4") & ", "
                            vartext += "A21 = " & x(1).ToString("N4")
                            vartext += "}"
                        Case "NRTL"
                            If doparallel Then
                                My.Application.IsRunningParallelTasks = True
                                proppack.Parameters("PP_FLASHALGORITHM") = 3
                                proppack.FlashAlgorithm = DWSIM.SimulationObjects.PropertyPackages.FlashMethod.InsideOut3P
                                proppack._tpcompids = New String() {currcase.comp1, currcase.comp2}
                                proppack._tpseverity = 0
                                Interfaces.ExcelIntegration.AddCompounds(proppack, New Object() {currcase.comp1, currcase.comp2})
                                Interfaces.ExcelIntegration.SetIP(proppack.ComponentName, proppack, New Object() {currcase.comp1, currcase.comp2}, New Double(,) {{0.0#, 0.0#}, {0.0#, 0.0#}}, New Double(,) {{0.0#, x(0)}, {x(1), 0.0#}}, New Double(,) {{0.0#, x(1)}, {x(0), 0.0#}}, New Double(,) {{0.0#, x(2)}, {x(2), 0.0#}})
                                Try
                                    Dim task1 As Task = Task.Factory.StartNew(Sub() Parallel.For(0, np, poptions,
                                                                             Sub(ipar)
                                                                                 Dim result2 As Object
                                                                                 result2 = proppack.FlashBase.Flash_PT(New Double() {0.5, 0.5}, VP(0), VT(ipar), proppack)
                                                                                 Vx1c(ipar) = result2(2)(0)
                                                                                 Vx2c(ipar) = result2(6)(0)
                                                                             End Sub))
                                    task1.Wait()
                                Catch ae As AggregateException
                                    For Each ex As Exception In ae.InnerExceptions
                                        Throw ex
                                    Next
                                End Try
                                My.Application.IsRunningParallelTasks = False
                            Else
                                proppack.Parameters("PP_FLASHALGORITHM") = 5
                                proppack.FlashAlgorithm = DWSIM.SimulationObjects.PropertyPackages.FlashMethod.GibbsMin3P
                                proppack._tpcompids = New String() {currcase.comp1, currcase.comp2}
                                proppack._tpseverity = 0
                                Interfaces.ExcelIntegration.AddCompounds(proppack, New Object() {currcase.comp1, currcase.comp2})
                                Interfaces.ExcelIntegration.SetIP(proppack.ComponentName, proppack, New Object() {currcase.comp1, currcase.comp2}, New Double(,) {{0.0#, 0.0#}, {0.0#, 0.0#}}, New Double(,) {{0.0#, x(0)}, {x(1), 0.0#}}, New Double(,) {{0.0#, x(1)}, {x(0), 0.0#}}, New Double(,) {{0.0#, x(2)}, {x(2), 0.0#}})
                                For i = 0 To np - 1
                                    result = proppack.FlashBase.Flash_PT(New Double() {0.5, 0.5}, VP(0), VT(i), proppack)
                                    Vx1c(i) = result(2)(0)
                                    Vx2c(i) = result(6)(0)
                                Next
                            End If
                            vartext = ", Interaction parameters = {"
                            vartext += "A12 = " & x(0).ToString("N4") & ", "
                            vartext += "A21 = " & x(1).ToString("N4") & ", "
                            vartext += "alpha12 = " & x(2).ToString("N4")
                            vartext += "}"
                        Case "Lee-Kesler-Plöcker", "Peng-Robinson", "Soave-Redlich-Kwong", "PC-SAFT"
                            If doparallel Then
                                My.Application.IsRunningParallelTasks = True
                                proppack.Parameters("PP_FLASHALGORITHM") = 3
                                proppack.FlashAlgorithm = DWSIM.SimulationObjects.PropertyPackages.FlashMethod.InsideOut3P
                                proppack._tpcompids = New String() {currcase.comp1, currcase.comp2}
                                proppack._tpseverity = 0
                                Interfaces.ExcelIntegration.AddCompounds(proppack, New Object() {currcase.comp1, currcase.comp2})
                                Interfaces.ExcelIntegration.SetIP(proppack.ComponentName, proppack, New Object() {currcase.comp1, currcase.comp2}, New Double(,) {{0.0#, x(0)}, {x(0), 0.0#}}, Nothing, Nothing, Nothing)
                                Try
                                    Dim task1 As Task = Task.Factory.StartNew(Sub() Parallel.For(0, np, poptions,
                                                                             Sub(ipar)
                                                                                 Dim result2 As Object
                                                                                 result2 = proppack.FlashBase.Flash_PT(New Double() {0.5, 0.5}, VP(0), VT(ipar), proppack)
                                                                                 Vx1c(ipar) = result2(2)(0)
                                                                                 Vx2c(ipar) = result2(6)(0)
                                                                             End Sub))
                                    task1.Wait()
                                Catch ae As AggregateException
                                    For Each ex As Exception In ae.InnerExceptions
                                        Throw ex
                                    Next
                                End Try
                                My.Application.IsRunningParallelTasks = False
                            Else
                                proppack.Parameters("PP_FLASHALGORITHM") = 5
                                proppack.FlashAlgorithm = DWSIM.SimulationObjects.PropertyPackages.FlashMethod.GibbsMin3P
                                proppack._tpcompids = New String() {currcase.comp1, currcase.comp2}
                                proppack._tpseverity = 0
                                Interfaces.ExcelIntegration.AddCompounds(proppack, New Object() {currcase.comp1, currcase.comp2})
                                Interfaces.ExcelIntegration.SetIP(proppack.ComponentName, proppack, New Object() {currcase.comp1, currcase.comp2}, New Double(,) {{0.0#, x(0)}, {x(0), 0.0#}}, Nothing, Nothing, Nothing)
                                For i = 0 To np - 1
                                    result = proppack.FlashBase.Flash_PT(New Double() {0.5, 0.5}, VP(0), VT(i), proppack)
                                    Vx1c(i) = result(2)(0)
                                    Vx2c(i) = result(6)(0)
                                Next
                            End If
                            vartext = ", Interaction parameters = {kij = "
                            For i = 0 To x.Length - 1
                                vartext += x(i).ToString("N4")
                            Next
                            vartext += "}"
                    End Select
                    For i = 0 To np - 1
                        Me.currcase.calcx1l1.Add(Vx1c(i))
                        Me.currcase.calcx1l2.Add(Vx2c(i))
                        Me.currcase.calct.Add(0.0#)
                        Me.currcase.calcp.Add(0.0#)
                        Me.currcase.calcy.Add(0.0#)
                        Select Case currcase.objfunction
                            Case "Least Squares (min T/P+y/x)"
                                f += ((Vx1c(i) - Vx1(i))) ^ 2 + ((Vx2c(i) - Vx2(i))) ^ 2
                            Case "Least Squares (min T/P)"
                                f += ((Vx1c(i) - Vx1(i))) ^ 2 + ((Vx2c(i) - Vx2(i))) ^ 2
                            Case "Least Squares (min y/x)"
                                f += ((Vx1c(i) - Vx1(i))) ^ 2 + ((Vx2c(i) - Vx2(i))) ^ 2
                            Case "Weighted Least Squares (min T/P+y/x)"
                                f += ((Vx1c(i) - Vx1(i)) / Vx1(i)) ^ 2 + ((Vx2c(i) - Vx2(i)) / Vx2(i)) ^ 2
                            Case "Weighted Least Squares (min T/P)"
                                f += ((Vx1c(i) - Vx1(i)) / Vx1(i)) ^ 2 + ((Vx2c(i) - Vx2(i)) / Vx2(i)) ^ 2
                            Case "Weighted Least Squares (min y/x)"
                                f += ((Vx1c(i) - Vx1(i)) / Vx1(i)) ^ 2 + ((Vx2c(i) - Vx2(i)) / Vx2(i)) ^ 2
                            Case "Chi Square"
                        End Select
                    Next
                Case DataType.TPxx
                    With proppack
                        ._tpseverity = 0
                        ._tpcompids = New String() {currcase.comp2}
                    End With
                    Select Case currcase.model
                        Case "PRSV2-M"
                            For i = 0 To np - 1
                                result = Interfaces.ExcelIntegration.PTFlash(proppack, 3, VP(i), VT(i), New Object() {currcase.comp1, currcase.comp2}, New Double() {0.5, 0.5}, New Double(,) {{0.0#, x(0)}, {x(0), 0.0#}}, New Double(,) {{0.0#, x(1)}, {x(1), 0.0#}}, Nothing, Nothing)
                                Vx1c(i) = result(2, 1)
                                Vx2c(i) = result(2, 2)
                            Next
                            vartext = ", Interaction parameters = {"
                            vartext += "kij = " & x(0).ToString("N4") & ", "
                            vartext += "kji = " & x(1).ToString("N4")
                            vartext += "}"
                        Case "PRSV2-VL"
                            For i = 0 To np - 1
                                result = Interfaces.ExcelIntegration.PTFlash(proppack, 3, VP(i), VT(i), New Object() {currcase.comp1, currcase.comp2}, New Double() {0.5, 0.5}, New Double(,) {{0.0#, x(0)}, {x(0), 0.0#}}, New Double(,) {{0.0#, x(1)}, {x(1), 0.0#}}, Nothing, Nothing)
                                Vx1c(i) = result(2, 1)
                                Vx2c(i) = result(2, 2)
                            Next
                            vartext = ", Interaction parameters = {"
                            vartext += "kij = " & x(0).ToString("N4") & ", "
                            vartext += "kji = " & x(1).ToString("N4")
                            vartext += "}"
                        Case "UNIQUAC"
                            For i = 0 To np - 1
                                result = Interfaces.ExcelIntegration.PTFlash(proppack, 3, VP(i), VT(i), New Object() {currcase.comp1, currcase.comp2}, New Double() {0.5, 0.5}, New Double(,) {{0.0#, 0.0#}, {0.0#, 0.0#}}, New Double(,) {{0.0#, x(0)}, {x(1), 0.0#}}, New Double(,) {{0.0#, x(1)}, {x(0), 0.0#}}, Nothing)
                                Vx1c(i) = result(2, 1)
                                Vx2c(i) = result(2, 2)
                            Next
                            vartext = ", Interaction parameters = {"
                            vartext += "A12 = " & x(0).ToString("N4") & ", "
                            vartext += "A21 = " & x(1).ToString("N4")
                            vartext += "}"
                        Case "NRTL"
                            For i = 0 To np - 1
                                result = Interfaces.ExcelIntegration.PTFlash(proppack, 3, VP(i), VT(i), New Object() {currcase.comp1, currcase.comp2}, New Double() {0.5, 0.5}, New Double(,) {{0.0#, 0.0#}, {0.0#, 0.0#}}, New Double(,) {{0.0#, x(0)}, {x(1), 0.0#}}, New Double(,) {{0.0#, x(1)}, {x(0), 0.0#}}, New Double(,) {{0.0#, x(2)}, {x(2), 0.0#}})
                                Vx1c(i) = result(2, 1)
                                Vx2c(i) = result(2, 2)
                            Next
                            vartext = ", Interaction parameters = {"
                            vartext += "A12 = " & x(0).ToString("N4") & ", "
                            vartext += "A21 = " & x(1).ToString("N4") & ", "
                            vartext += "alpha12 = " & x(2).ToString("N4")
                            vartext += "}"
                        Case "Lee-Kesler-Plöcker"
                            For i = 0 To np - 1
                                result = Interfaces.ExcelIntegration.PTFlash(proppack, 3, VP(i), VT(i), New Object() {currcase.comp1, currcase.comp2}, New Double() {0.5, 0.5}, New Double(,) {{0.0#, x(0)}, {x(0), 0.0#}}, Nothing, Nothing, Nothing)
                                Vx1c(i) = result(2, 1)
                                Vx2c(i) = result(2, 2)
                            Next
                            vartext = ", Interaction parameters = {kij = "
                            For i = 0 To x.Length - 1
                                vartext += x(i).ToString("N4")
                            Next
                            vartext += "}"
                        Case "Peng-Robinson"
                            For i = 0 To np - 1
                                result = Interfaces.ExcelIntegration.PTFlash(proppack, 3, VP(i), VT(i), New Object() {currcase.comp1, currcase.comp2}, New Double() {0.5, 0.5}, New Double(,) {{0.0#, x(0)}, {x(0), 0.0#}}, Nothing, Nothing, Nothing)
                                Vx1c(i) = result(2, 1)
                                Vx2c(i) = result(2, 2)
                            Next
                            vartext = ", Interaction parameters = {kij = "
                            For i = 0 To x.Length - 1
                                vartext += x(i).ToString("N4")
                            Next
                            vartext += "}"
                        Case "Soave-Redlich-Kwong"
                            For i = 0 To np - 1
                                result = Interfaces.ExcelIntegration.PTFlash(proppack, 3, VP(i), VT(i), New Object() {currcase.comp1, currcase.comp2}, New Double() {0.5, 0.5}, New Double(,) {{0.0#, x(0)}, {x(0), 0.0#}}, Nothing, Nothing, Nothing)
                                Vx1c(i) = result(2, 1)
                                Vx2c(i) = result(2, 2)
                            Next
                            vartext = ", Interaction parameters = {kij = "
                            For i = 0 To x.Length - 1
                                vartext += x(i).ToString("N4")
                            Next
                            vartext += "}"
                        Case "PC-SAFT"
                            For i = 0 To np - 1
                                result = Interfaces.ExcelIntegration.PTFlash(proppack, 3, VP(i), VT(i), New Object() {currcase.comp1, currcase.comp2}, New Double() {0.5, 0.5}, New Double(,) {{0.0#, x(0)}, {x(0), 0.0#}}, Nothing, Nothing, Nothing)
                                Vx1c(i) = result(2, 1)
                                Vx2c(i) = result(2, 2)
                            Next
                            vartext = ", Interaction parameters = {kij = "
                            For i = 0 To x.Length - 1
                                vartext += x(i).ToString("N4")
                            Next
                            vartext += "}"
                    End Select
                    For i = 0 To np - 1
                        Me.currcase.calcx1l1.Add(Vx1c(i))
                        Me.currcase.calcx1l2.Add(Vx2c(i))
                        Me.currcase.calct.Add(0.0#)
                        Me.currcase.calcp.Add(0.0#)
                        Me.currcase.calcy.Add(0.0#)
                        Select Case currcase.objfunction
                            Case "Least Squares (min T/P+y/x)"
                                f += ((Vx1c(i) - Vx1(i))) ^ 2 + ((Vx2c(i) - Vx2(i))) ^ 2
                            Case "Least Squares (min T/P)"
                                f += ((Vx1c(i) - Vx1(i))) ^ 2 + ((Vx2c(i) - Vx2(i))) ^ 2
                            Case "Least Squares (min y/x)"
                                f += ((Vx1c(i) - Vx1(i))) ^ 2 + ((Vx2c(i) - Vx2(i))) ^ 2
                            Case "Weighted Least Squares (min T/P+y/x)"
                                f += ((Vx1c(i) - Vx1(i)) / Vx1(i)) ^ 2 + ((Vx2c(i) - Vx2(i)) / Vx2(i)) ^ 2
                            Case "Weighted Least Squares (min T/P)"
                                f += ((Vx1c(i) - Vx1(i)) / Vx1(i)) ^ 2 + ((Vx2c(i) - Vx2(i)) / Vx2(i)) ^ 2
                            Case "Weighted Least Squares (min y/x)"
                                f += ((Vx1c(i) - Vx1(i)) / Vx1(i)) ^ 2 + ((Vx2c(i) - Vx2(i)) / Vx2(i)) ^ 2
                            Case "Chi Square"
                        End Select
                    Next
            End Select

            itn += 1
            Me.tbRegResults.AppendText("Iteration #" & itn & ", Function Value = " & Format(f, "E") & vartext & vbCrLf)

            UpdateData()
            Application.DoEvents()

        Catch ex As Exception

            itn += 1
            Me.tbRegResults.AppendText("Iteration #" & itn & ", Exception: " & ex.Message & vbCrLf)

        End Try

        Return f

    End Function

    Private Function FunctionGradient(ByVal x() As Double) As Double()

        Application.DoEvents()
        If cancel Then
            Return x
            Exit Function
        End If

        Dim g(x.Length - 1) As Double

        Dim epsilon As Double = 0.00001

        Dim f2(x.Length - 1), f3(x.Length - 1) As Double
        Dim x2(x.Length - 1), x3(x.Length - 1) As Double
        Dim i, j As Integer

        For i = 0 To x.Length - 1
            For j = 0 To x.Length - 1
                x2(j) = x(j)
                x3(j) = x(j)
            Next
            x2(i) = x(i) + epsilon
            x3(i) = x(i) - epsilon
            f2(i) = FunctionValue(x2)
            f3(i) = FunctionValue(x3)
            g(i) = (f2(i) - f3(i)) / (x2(i) - x3(i))
        Next

        Return g

    End Function

    'IPOPT

    Public Function eval_f(ByVal n As Integer, ByVal x As Double(), ByVal new_x As Boolean, ByRef obj_value As Double) As Boolean
        Dim fval As Double = FunctionValue(x)
        obj_value = fval
        Return True
    End Function

    Public Function eval_grad_f(ByVal n As Integer, ByVal x As Double(), ByVal new_x As Boolean, ByRef grad_f As Double()) As Boolean
        Dim g As Double() = FunctionGradient(x)
        grad_f = g
        Return True
    End Function

    Public Function eval_g(ByVal n As Integer, ByVal x As Double(), ByVal new_x As Boolean, ByVal m As Integer, ByRef g As Double()) As Boolean
        'g(0) = x(0) * x(1) * x(2) * x(3)
        'g(1) = x(0) * x(0) + x(1) * x(1) + x(2) * x(2) + x(3) * x(3)
        Return True
    End Function

    Public Function eval_jac_g(ByVal n As Integer, ByVal x As Double(), ByVal new_x As Boolean, ByVal m As Integer, ByVal nele_jac As Integer, ByRef iRow As Integer(), _
ByRef jCol As Integer(), ByRef values As Double()) As Boolean
        If values Is Nothing Then
            ' set the structure of the jacobian 
            ' this particular jacobian is dense 
            'iRow(0) = 0
            'jCol(0) = 0
            'iRow(1) = 0
            'jCol(1) = 1
            'iRow(2) = 0
            'jCol(2) = 2
            'iRow(3) = 0
            'jCol(3) = 3
            'iRow(4) = 1
            'jCol(4) = 0
            'iRow(5) = 1
            'jCol(5) = 1
            'iRow(6) = 1
            'jCol(6) = 2
            'iRow(7) = 1
            'jCol(7) = 3
        Else
            '' return the values of the jacobian of the constraints 
            'values(0) = x(1) * x(2) * x(3)  ' 0,0 
            'values(1) = x(0) * x(2) * x(3)  ' 0,1 
            'values(2) = x(0) * x(1) * x(3)  ' 0,2 
            'values(3) = x(0) * x(1) * x(2)  ' 0,3 

            'values(4) = 2 * x(0)            ' 1,0 
            'values(5) = 2 * x(1)            ' 1,1 
            'values(6) = 2 * x(2)            ' 1,2 
            'values(7) = 2 * x(3)            ' 1,3 
        End If

        Return False
    End Function

    Public Function eval_h(ByVal n As Integer, ByVal x As Double(), ByVal new_x As Boolean, ByVal obj_factor As Double, ByVal m As Integer, ByVal lambda As Double(), _
ByVal new_lambda As Boolean, ByVal nele_hess As Integer, ByRef iRow As Integer(), ByRef jCol As Integer(), ByRef values As Double()) As Boolean
        Return False
    End Function

    Private Sub btnDoReg_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDoReg.Click

        Me.tbRegResults.Clear()

        cancel = False

        Me.btnDoReg.Enabled = False
        Me.btnCancel.Enabled = True

        Try

            currcase = Me.StoreCase()

            Dim nvar As Integer = 0
            Dim i As Integer

            Dim initval2() As Double = Nothing
            Dim lconstr2() As Double = Nothing
            Dim uconstr2() As Double = Nothing
            Dim finalval2() As Double = Nothing

            Select Case currcase.model
                Case "PC-SAFT"
                    initval2 = New Double() {currcase.iepar1}
                    lconstr2 = New Double() {-0.5#}
                    uconstr2 = New Double() {0.5#}
                    nvar = 1
                Case "Peng-Robinson"
                    initval2 = New Double() {currcase.iepar1}
                    lconstr2 = New Double() {-0.5#}
                    uconstr2 = New Double() {0.5#}
                    nvar = 1
                Case "PRSV2-M", "PRSV2-VL"
                    nvar = 2
                    initval2 = New Double() {currcase.iepar1, currcase.iepar2}
                    lconstr2 = New Double() {-0.5#, -0.5#}
                    uconstr2 = New Double() {0.5#, 0.5#}
                Case "Soave-Redlich-Kwong"
                    initval2 = New Double() {currcase.iepar1}
                    lconstr2 = New Double() {-0.5#}
                    uconstr2 = New Double() {0.5#}
                    nvar = 1
                Case "UNIQUAC"
                    nvar = 2
                    initval2 = New Double() {currcase.iepar1, currcase.iepar2}
                    lconstr2 = New Double() {-3000000.0#, -3000000.0#}
                    uconstr2 = New Double() {3000000.0#, 3000000.0#}
                Case "NRTL"
                    nvar = 3
                    initval2 = New Double() {currcase.iepar1, currcase.iepar2, currcase.iepar3}
                    lconstr2 = New Double() {-3000000.0#, -3000000.0#, 0.0#}
                    uconstr2 = New Double() {3000000.0#, 3000000.0#, 0.8#}
                Case "Lee-Kesler-Plöcker"
                    initval2 = New Double() {1.0#}
                    lconstr2 = New Double() {0.9#}
                    uconstr2 = New Double() {1.1#}
                    nvar = 1
            End Select

            itn = 0

            Me.tbRegResults.AppendText("Starting experimental data regression for " & currcase.model & " model parameter estimation..." & vbCrLf)

            Dim ppm As New CAPEOPENPropertyPackageManager()

            Select Case currcase.model
                Case "PC-SAFT"
                    ppname = "PC-SAFT"
                Case "Peng-Robinson"
                    ppname = "Peng-Robinson (PR)"
                Case "Soave-Redlich-Kwong"
                    ppname = "Soave-Redlich-Kwong (SRK)"
                Case "UNIQUAC"
                    ppname = "UNIQUAC"
                Case "PRSV2-M"
                    ppname = "Peng-Robinson-Stryjek-Vera 2 (PRSV2-M)"
                Case "PRSV2-VL"
                    ppname = "Peng-Robinson-Stryjek-Vera 2 (PRSV2-VL)"
                Case "NRTL"
                    ppname = "NRTL"
                Case "Lee-Kesler-Plöcker"
                    ppname = "Lee-Kesler-Plöcker"
            End Select

            proppack = ppm.GetPropertyPackage(ppname)
            proppack.ComponentName = ppname
            proppack._availablecomps = FormMain.AvailableComponents

            Select Case currcase.method
                Case "Limited Memory BFGS"
                    Dim variables(nvar - 1) As Optimization.OptBoundVariable
                    For i = 0 To nvar - 1
                        variables(i) = New Optimization.OptBoundVariable("x" & CStr(i + 1), initval2(i), lconstr2(i), uconstr2(i))
                    Next
                    Dim solver As New Optimization.L_BFGS_B
                    solver.Tolerance = currcase.tolerance
                    solver.MaxFunEvaluations = currcase.maxits
                    solver.ComputeMin(AddressOf FunctionValue, AddressOf FunctionGradient, variables)
                Case "Truncated Newton"
                    Dim variables(nvar - 1) As Optimization.OptBoundVariable
                    For i = 0 To nvar - 1
                        variables(i) = New Optimization.OptBoundVariable("x" & CStr(i + 1), initval2(i), lconstr2(i), uconstr2(i))
                    Next
                    Dim solver As New Optimization.TruncatedNewton
                    solver.Tolerance = currcase.tolerance
                    solver.MaxFunEvaluations = currcase.maxits
                    solver.ComputeMin(AddressOf FunctionValue, AddressOf FunctionGradient, variables)
                Case "Nelder-Mead Simplex Downhill"
                    Dim variables(nvar - 1) As Optimization.OptBoundVariable
                    For i = 0 To nvar - 1
                        variables(i) = New Optimization.OptBoundVariable("x" & CStr(i + 1), initval2(i), lconstr2(i), uconstr2(i))
                    Next
                    Dim solver As New Optimization.Simplex
                    solver.Tolerance = currcase.tolerance
                    solver.MaxFunEvaluations = currcase.maxits
                    solver.ComputeMin(AddressOf FunctionValue, variables)
                Case "IPOPT"
                    Dim obj As Double
                    Dim status As IpoptReturnCode
                    Using problem As New Ipopt(initval2.Length, lconstr2, uconstr2, 0, Nothing, Nothing, _
                     0, 0, AddressOf eval_f, AddressOf eval_g, _
                     AddressOf eval_grad_f, AddressOf eval_jac_g, AddressOf eval_h)
                        problem.AddOption("tol", currcase.tolerance)
                        problem.AddOption("max_iter", currcase.maxits)
                        problem.AddOption("mu_strategy", "adaptive")
                        problem.AddOption("hessian_approximation", "limited-memory")
                        'solve the problem 
                        status = problem.SolveProblem(initval2, obj, Nothing, Nothing, Nothing, Nothing)
                    End Using
            End Select

            Me.tbRegResults.AppendText("Finished!")

        Catch ex As Exception

            Me.tbRegResults.AppendText(ex.ToString)

        Finally

            currcase.results = Me.tbRegResults.Text

            Me.btnDoReg.Enabled = True
            Me.btnCancel.Enabled = False

        End Try

    End Sub

    Sub UpdateData()

        Dim i As Integer = 0
        With Me.currcase
            px = New ArrayList
            px2 = New ArrayList
            px3 = New ArrayList
            px4 = New ArrayList
            py1 = New ArrayList
            py2 = New ArrayList
            py3 = New ArrayList
            py4 = New ArrayList
            py5 = New ArrayList
            ycurvetypes = New ArrayList
            xformat = 1
            title = tbTitle.Text & " / " & .datatype.ToString
            Select Case .datatype
                Case DataType.Txy
                    For i = 0 To .x1p.Count - 1
                        Try
                            px.Add(Double.Parse(.x1p(i)))
                            py1.Add(Double.Parse(.tp(i)))
                            py2.Add(cv.ConverterDoSI(.tunit, .calct(i)))
                            py4.Add(cv.ConverterDoSI(.tunit, .calct(i)))
                            px2.Add(Double.Parse(.yp(i)))
                            py3.Add(Double.Parse(.tp(i)))
                            py5.Add(Double.Parse(.calcy(i)))
                        Catch ex As Exception
                        End Try
                    Next
                    xtitle = "Liquid Phase Mole Fraction " & .comp1
                    ytitle = "T / " & .tunit
                    y2title = "Vapor Phase Mole Fraction " & .comp1
                    y1ctitle = "Tx exp."
                    y2ctitle = "Tx calc."
                    y3ctitle = "Ty exp."
                    y4ctitle = "Ty calc."
                    y5ctitle = "y exp."
                    y6ctitle = "y calc."
                    ycurvetypes.AddRange(New Integer() {1, 3, 1, 3, 1, 3})
                Case DataType.Pxy
                    For i = 0 To .x1p.Count - 1
                        Try
                            px.Add(Double.Parse(.x1p(i)))
                            py1.Add(Double.Parse(.pp(i)))
                            py2.Add(cv.ConverterDoSI(.punit, .calcp(i)))
                            py4.Add(cv.ConverterDoSI(.punit, .calcp(i)))
                            px2.Add(Double.Parse(.yp(i)))
                            py3.Add(Double.Parse(.pp(i)))
                            py5.Add(Double.Parse(.calcy(i)))
                        Catch ex As Exception
                        End Try
                    Next
                    xtitle = "Liquid Phase Mole Fraction " & .comp1
                    ytitle = "P / " & .punit
                    y2title = "Vapor Phase Mole Fraction " & .comp1
                    y1ctitle = "Px exp."
                    y2ctitle = "Px calc."
                    y3ctitle = "Py exp."
                    y4ctitle = "Py calc."
                    y5ctitle = "y exp."
                    y6ctitle = "y calc."
                    ycurvetypes.AddRange(New Integer() {1, 3, 1, 3, 1, 3})
                Case DataType.TPxy
                    For i = 0 To .x1p.Count - 1
                        Try
                            px.Add(Double.Parse(.x1p(i)))
                            py1.Add(Double.Parse(.tp(i)))
                            py2.Add(cv.ConverterDoSI(.tunit, .calct(i)))
                            py4.Add(cv.ConverterDoSI(.punit, .calcp(i)))
                            py3.Add(Double.Parse(.pp(i)))
                            py5.Add(Double.Parse(.calcy(i)))
                        Catch ex As Exception
                        End Try
                    Next
                    xtitle = "Liquid Phase Mole Fraction " & .comp1
                    ytitle = "T / " & .tunit & " - P / " & .punit
                    y2title = "Vapor Phase Mole Fraction " & .comp1
                    y5ctitle = "y exp."
                    y6ctitle = "y calc."
                    ycurvetypes.AddRange(New Integer() {1, 3, 1, 3, 1, 3})
                Case DataType.Txx
                    Try
                        For i = 0 To .x1p.Count - 1
                            px.Add(Double.Parse(.x1p(i)))
                            py1.Add(Double.Parse(.tp(i)))
                            px2.Add(Double.Parse(.x2p(i)))
                            py2.Add(Double.Parse(.tp(i)))
                            px3.Add(Double.Parse(.calcx1l1(i)))
                            py3.Add(Double.Parse(.tp(i)))
                            px4.Add(Double.Parse(.calcx1l2(i)))
                            py4.Add(Double.Parse(.tp(i)))
                        Next
                    Catch ex As Exception

                    End Try
                    xtitle = "Mole Fraction " & .comp1
                    ytitle = "T / " & .tunit
                    y1ctitle = "Tx1' exp."
                    y3ctitle = "Tx1' calc."
                    y2ctitle = "Tx1'' exp."
                    y4ctitle = "Tx1'' calc."
                    ycurvetypes.AddRange(New Integer() {1, 1, 3, 3})
                Case DataType.Pxx
                    Try
                        For i = 0 To .x1p.Count - 1
                            px.Add(Double.Parse(.x1p(i)))
                            py1.Add(Double.Parse(.pp(i)))
                            px2.Add(Double.Parse(.x2p(i)))
                            py2.Add(Double.Parse(.pp(i)))
                            px3.Add(Double.Parse(.calcx1l1(i)))
                            py3.Add(Double.Parse(.pp(i)))
                            px4.Add(Double.Parse(.calcx1l2(i)))
                            py4.Add(Double.Parse(.pp(i)))
                        Next
                    Catch ex As Exception

                    End Try
                    xtitle = "Mole Fraction " & .comp1
                    ytitle = "P / " & .tunit
                    y1ctitle = "Px1' exp."
                    y3ctitle = "Px1' calc."
                    y2ctitle = "Px1'' exp."
                    y4ctitle = "Px1'' calc."
                    ycurvetypes.AddRange(New Integer() {1, 1, 3, 3})
                Case DataType.TPxx
                    Try
                        For i = 0 To .x1p.Count - 1
                            px.Add(Double.Parse(.x1p(i)))
                            py1.Add(Double.Parse(.tp(i)))
                            px2.Add(Double.Parse(.x2p(i)))
                            py2.Add(Double.Parse(.tp(i)))
                            px3.Add(Double.Parse(.calcx1l1(i)))
                            py3.Add(Double.Parse(.tp(i)))
                            px4.Add(Double.Parse(.calcx1l2(i)))
                            py4.Add(Double.Parse(.tp(i)))
                        Next
                    Catch ex As Exception

                    End Try
                    xtitle = "Mole Fraction " & .comp1
                    ytitle = "T / " & .tunit & " - P / " & .punit
                    y1ctitle = "Tx1' exp."
                    y3ctitle = "Tx1' calc."
                    y2ctitle = "Tx1'' exp."
                    y4ctitle = "Tx1'' calc."
                    ycurvetypes.AddRange(New Integer() {1, 1, 3, 3})
            End Select
        End With

        UpdateTable()
        DrawChart()

    End Sub

    Sub UpdateTable()

        Me.gridstats.Rows.Clear()
        For i As Integer = 0 To currcase.x1p.Count - 1
            With currcase
                Try
                    Me.gridstats.Rows.Add(New Object() {.x1p(i), .calcx1l1(i), .x2p(i), .calcx1l2(i), .yp(i), .calcy(i), .tp(i), cv.ConverterDoSI(.tunit, .calct(i)), .pp(i), cv.ConverterDoSI(.punit, .calcp(i)), _
                                                        .calcy(i) - .yp(i), (.calcy(i) - .yp(i)) / .yp(i), (.calcy(i) - .yp(i)) / .yp(i) * 100, _
                                                       cv.ConverterDoSI(.punit, .calcp(i)) - .pp(i), (cv.ConverterDoSI(.punit, .calcp(i)) - .pp(i)) / .pp(i), (cv.ConverterDoSI(.punit, .calcp(i)) - .pp(i)) / .pp(i) * 100, _
                                                        cv.ConverterDoSI(.tunit, .calct(i)) - .tp(i), (cv.ConverterDoSI(.tunit, .calct(i)) - .tp(i)) / .tp(i), (cv.ConverterDoSI(.tunit, .calct(i)) - .tp(i)) / .tp(i) * 100, _
                                                        .calcx1l1(i) - .x1p(i), (.calcx1l1(i) - .x1p(i)) / .x1p(i), (.calcx1l1(i) - .x1p(i)) / .x1p(i) * 100, _
                                                        .calcx1l2(i) - .x2p(i), (.calcx1l2(i) - .x2p(i)) / .x2p(i), (.calcx1l2(i) - .x2p(i)) / .x2p(i) * 100})
                Catch ex As Exception

                End Try
            End With
        Next

        Select Case currcase.datatype
            Case DataType.Txx, DataType.Pxx, DataType.TPxx
                Me.gridstats.Columns(0).Visible = True
                Me.gridstats.Columns(1).Visible = True
                Me.gridstats.Columns(2).Visible = True
                Me.gridstats.Columns(3).Visible = True
                Me.gridstats.Columns(4).Visible = False
                Me.gridstats.Columns(5).Visible = False
                Me.gridstats.Columns(6).Visible = False
                Me.gridstats.Columns(7).Visible = False
                Me.gridstats.Columns(8).Visible = False
                Me.gridstats.Columns(9).Visible = False
                Me.gridstats.Columns(10).Visible = False
                Me.gridstats.Columns(11).Visible = False
                Me.gridstats.Columns(12).Visible = False
                Me.gridstats.Columns(13).Visible = False
                Me.gridstats.Columns(14).Visible = False
                Me.gridstats.Columns(15).Visible = False
                Me.gridstats.Columns(16).Visible = False
                Me.gridstats.Columns(17).Visible = False
                Me.gridstats.Columns(18).Visible = False
                Me.gridstats.Columns(19).Visible = True
                Me.gridstats.Columns(20).Visible = True
                Me.gridstats.Columns(21).Visible = True
                Me.gridstats.Columns(22).Visible = True
                Me.gridstats.Columns(23).Visible = True
                Me.gridstats.Columns(24).Visible = True
            Case DataType.Pxy
                Me.gridstats.Columns(0).Visible = True
                Me.gridstats.Columns(1).Visible = False
                Me.gridstats.Columns(2).Visible = False
                Me.gridstats.Columns(3).Visible = False
                Me.gridstats.Columns(4).Visible = True
                Me.gridstats.Columns(5).Visible = True
                Me.gridstats.Columns(6).Visible = True
                Me.gridstats.Columns(7).Visible = False
                Me.gridstats.Columns(8).Visible = True
                Me.gridstats.Columns(9).Visible = True
                Me.gridstats.Columns(10).Visible = True
                Me.gridstats.Columns(11).Visible = True
                Me.gridstats.Columns(12).Visible = True
                Me.gridstats.Columns(13).Visible = True
                Me.gridstats.Columns(14).Visible = True
                Me.gridstats.Columns(15).Visible = True
                Me.gridstats.Columns(16).Visible = False
                Me.gridstats.Columns(17).Visible = False
                Me.gridstats.Columns(18).Visible = False
                Me.gridstats.Columns(19).Visible = False
                Me.gridstats.Columns(20).Visible = False
                Me.gridstats.Columns(21).Visible = False
                Me.gridstats.Columns(22).Visible = False
                Me.gridstats.Columns(23).Visible = False
                Me.gridstats.Columns(24).Visible = False
            Case DataType.Txy
                Me.gridstats.Columns(0).Visible = True
                Me.gridstats.Columns(1).Visible = False
                Me.gridstats.Columns(2).Visible = False
                Me.gridstats.Columns(3).Visible = False
                Me.gridstats.Columns(4).Visible = True
                Me.gridstats.Columns(5).Visible = True
                Me.gridstats.Columns(6).Visible = True
                Me.gridstats.Columns(7).Visible = True
                Me.gridstats.Columns(8).Visible = True
                Me.gridstats.Columns(9).Visible = False
                Me.gridstats.Columns(10).Visible = True
                Me.gridstats.Columns(11).Visible = True
                Me.gridstats.Columns(12).Visible = True
                Me.gridstats.Columns(13).Visible = False
                Me.gridstats.Columns(14).Visible = False
                Me.gridstats.Columns(15).Visible = False
                Me.gridstats.Columns(16).Visible = True
                Me.gridstats.Columns(17).Visible = True
                Me.gridstats.Columns(18).Visible = True
                Me.gridstats.Columns(19).Visible = False
                Me.gridstats.Columns(20).Visible = False
                Me.gridstats.Columns(21).Visible = False
                Me.gridstats.Columns(22).Visible = False
                Me.gridstats.Columns(23).Visible = False
                Me.gridstats.Columns(24).Visible = False
        End Select


    End Sub

    Public px, px2, px3, px4, py1, py2, py3, py4, py5 As ArrayList
    Public xtitle, ytitle, y2title, title, y1ctitle, y2ctitle, y3ctitle, y4ctitle, y5ctitle, y6ctitle As String
    Public ycurvetypes As ArrayList
    Public xformat As Integer

    'xformat:
    '1 - double number
    '2 - integer
    '3 - date (dd/MM)

    'ycurvetypes:
    '1 - points only
    '2 - points and line
    '3 - line only
    '4 - dashed line
    '5 - dashed line with points
    '6 - non-smoothed line

    Sub DrawChart()

        Dim rnd As New Random()

        With graph.GraphPane
            .GraphObjList.Clear()
            .CurveList.Clear()
            .YAxisList.Clear()
            If py1.Count > 0 Then
                Dim ya0 As New ZedGraph.YAxis(ytitle)
                ya0.Scale.FontSpec.Size = 10
                ya0.Title.FontSpec.Size = 11
                .YAxisList.Add(ya0)
                Dim mycurve As LineItem = Nothing
                Select Case currcase.datatype
                    Case DataType.Txy, DataType.Pxy, DataType.TPxy
                        mycurve = .AddCurve(y1ctitle, px.ToArray(GetType(Double)), py1.ToArray(GetType(Double)), Color.Black)
                    Case DataType.Txy, DataType.Pxy
                        mycurve = .AddCurve(y1ctitle, px.ToArray(GetType(Double)), py1.ToArray(GetType(Double)), Color.Black)
                    Case DataType.TPxx, DataType.Txx, DataType.Pxx
                        mycurve = .AddCurve(y1ctitle, px.ToArray(GetType(Double)), py1.ToArray(GetType(Double)), Color.Black)
                End Select
                With mycurve
                    Dim ppl As ZedGraph.PointPairList = .Points
                    ppl.Sort(ZedGraph.SortType.XValues)
                    Select Case ycurvetypes(0)
                        '1 - somente pontos
                        '2 - pontos e linha
                        '3 - somente linha
                        '4 - linha tracejada
                        '5 - linha tracejada com pontos
                        Case 1
                            .Line.IsVisible = False
                            .Line.IsSmooth = False
                            .Color = Color.Blue
                            .Symbol.Type = ZedGraph.SymbolType.Circle
                            .Symbol.Fill.Type = ZedGraph.FillType.Solid
                            .Symbol.Fill.Color = Color.Blue
                            .Symbol.Fill.IsVisible = True
                            .Symbol.Size = 5
                        Case 2
                            .Line.IsVisible = True
                            .Line.IsSmooth = False
                            .Color = Color.Blue
                            .Symbol.Type = ZedGraph.SymbolType.Circle
                            .Symbol.Fill.Type = ZedGraph.FillType.Solid
                            .Symbol.Fill.Color = Color.Blue
                            .Symbol.Fill.IsVisible = True
                            .Symbol.Size = 5
                        Case 3
                            .Line.IsVisible = True
                            .Line.IsSmooth = True
                            .Color = Color.Blue
                            .Symbol.IsVisible = False
                        Case 4
                            .Line.IsVisible = True
                            .Line.IsSmooth = False
                            .Line.Style = Drawing2D.DashStyle.Dash
                            .Color = Color.Blue
                            .Symbol.IsVisible = False
                        Case 5
                            .Line.IsVisible = True
                            .Line.IsSmooth = False
                            .Line.Style = Drawing2D.DashStyle.Dash
                            .Color = Color.Blue
                            .Symbol.Type = ZedGraph.SymbolType.Circle
                            .Symbol.Fill.Type = ZedGraph.FillType.Solid
                            .Symbol.Fill.Color = Color.Blue
                            .Symbol.Fill.IsVisible = True
                            .Symbol.Size = 5
                        Case 6
                            .Line.IsVisible = True
                            .Line.IsSmooth = False
                            Dim c1 As Color = Color.FromArgb(255, rnd.Next(0, 255), rnd.Next(0, 255), rnd.Next(0, 255))
                            .Color = Color.Blue
                            .Symbol.IsVisible = False
                    End Select
                    .YAxisIndex = 0
                End With
            End If
            If Not py2 Is Nothing Or Not px2 Is Nothing Then
                If py2.Count > 0 Or px2.Count > 0 Then
                    Dim mycurve As LineItem = Nothing
                    Select Case currcase.datatype
                        Case DataType.Txy, DataType.Pxy, DataType.TPxy
                            mycurve = .AddCurve(y2ctitle, px.ToArray(GetType(Double)), py2.ToArray(GetType(Double)), Color.Black)
                        Case DataType.Txy, DataType.Pxy
                            mycurve = .AddCurve(y2ctitle, px2.ToArray(GetType(Double)), py1.ToArray(GetType(Double)), Color.Black)
                        Case DataType.TPxx, DataType.Txx, DataType.Pxx
                            mycurve = .AddCurve(y2ctitle, px2.ToArray(GetType(Double)), py2.ToArray(GetType(Double)), Color.Black)
                    End Select
                    With mycurve
                        Dim ppl As ZedGraph.PointPairList = .Points
                        ppl.Sort(ZedGraph.SortType.XValues)
                        Select Case ycurvetypes(1)
                            '1 - somente pontos
                            '2 - pontos e linha
                            '3 - somente linha
                            '4 - linha tracejada
                            '5 - linha tracejada com pontos
                            Case 1
                                .Line.IsVisible = False
                                .Line.IsSmooth = False
                                .Color = Color.LightBlue
                                .Symbol.Type = ZedGraph.SymbolType.Circle
                                .Symbol.Fill.Type = ZedGraph.FillType.Solid
                                .Symbol.Fill.Color = Color.LightBlue
                                .Symbol.Fill.IsVisible = True
                                .Symbol.Size = 5
                            Case 2
                                .Line.IsVisible = True
                                .Line.IsSmooth = False
                                .Color = Color.LightBlue
                                .Symbol.Type = ZedGraph.SymbolType.Circle
                                .Symbol.Fill.Type = ZedGraph.FillType.Solid
                                .Symbol.Fill.Color = Color.LightBlue
                                .Symbol.Fill.IsVisible = True
                                .Symbol.Size = 5
                            Case 3
                                .Line.IsVisible = True
                                .Line.IsSmooth = True
                                .Color = Color.LightBlue
                                .Symbol.IsVisible = False
                            Case 4
                                .Line.IsVisible = True
                                .Line.IsSmooth = False
                                .Line.Style = Drawing2D.DashStyle.Dash
                                .Color = Color.LightBlue
                                .Symbol.IsVisible = False
                            Case 5
                                .Line.IsVisible = True
                                .Line.IsSmooth = False
                                .Line.Style = Drawing2D.DashStyle.Dash
                                .Color = Color.LightBlue
                                .Symbol.Type = ZedGraph.SymbolType.Circle
                                .Symbol.Fill.Type = ZedGraph.FillType.Solid
                                .Symbol.Fill.Color = Color.LightBlue
                                .Symbol.Fill.IsVisible = True
                                .Symbol.Size = 5
                            Case 6
                                .Line.IsVisible = True
                                .Line.IsSmooth = False
                                .Color = Color.LightBlue
                                .Symbol.IsVisible = False
                        End Select
                        .YAxisIndex = 0
                    End With
                End If
            End If
            If Not py3 Is Nothing Or Not px3 Is Nothing Then
                If py3.Count > 0 Or px3.Count > 0 Then
                    Dim mycurve As LineItem = Nothing
                    Select Case currcase.datatype
                        Case DataType.Txy, DataType.Pxy, DataType.TPxy
                            mycurve = .AddCurve(y3ctitle, px2.ToArray(GetType(Double)), py3.ToArray(GetType(Double)), Color.Black)
                        Case DataType.Txy, DataType.Pxy
                            mycurve = .AddCurve(y3ctitle, px3.ToArray(GetType(Double)), py1.ToArray(GetType(Double)), Color.Black)
                        Case DataType.TPxx, DataType.Txx, DataType.Pxx
                            mycurve = .AddCurve(y3ctitle, px3.ToArray(GetType(Double)), py3.ToArray(GetType(Double)), Color.Black)
                    End Select
                    With mycurve
                        Dim ppl As ZedGraph.PointPairList = .Points
                        ppl.Sort(ZedGraph.SortType.XValues)
                        Select Case ycurvetypes(2)
                            '1 - somente pontos
                            '2 - pontos e linha
                            '3 - somente linha
                            '4 - linha tracejada
                            '5 - linha tracejada com pontos
                            Case 1
                                .Line.IsVisible = False
                                .Line.IsSmooth = False
                                .Color = Color.Red
                                .Symbol.Type = ZedGraph.SymbolType.Circle
                                .Symbol.Fill.Type = ZedGraph.FillType.Solid
                                .Symbol.Fill.Color = Color.Red
                                .Symbol.Fill.IsVisible = True
                                .Symbol.Size = 5
                            Case 2
                                .Line.IsVisible = True
                                .Line.IsSmooth = False
                                .Color = Color.Red
                                .Symbol.Type = ZedGraph.SymbolType.Circle
                                .Symbol.Fill.Type = ZedGraph.FillType.Solid
                                .Symbol.Fill.Color = Color.Red
                                .Symbol.Fill.IsVisible = True
                                .Symbol.Size = 5
                            Case 3
                                .Line.IsVisible = True
                                .Line.IsSmooth = True
                                .Color = Color.Red
                                .Symbol.IsVisible = False
                            Case 4
                                .Line.IsVisible = True
                                .Line.IsSmooth = False
                                .Line.Style = Drawing2D.DashStyle.Dash
                                .Color = Color.Red
                                .Symbol.IsVisible = False
                            Case 5
                                .Line.IsVisible = True
                                .Line.IsSmooth = False
                                .Line.Style = Drawing2D.DashStyle.Dash
                                .Color = Color.Red
                                .Symbol.Type = ZedGraph.SymbolType.Circle
                                .Symbol.Fill.Type = ZedGraph.FillType.Solid
                                .Symbol.Fill.Color = Color.Red
                                .Symbol.Fill.IsVisible = True
                                .Symbol.Size = 5
                            Case 6
                                .Line.IsVisible = True
                                .Line.IsSmooth = False
                                .Color = Color.Red
                                .Symbol.IsVisible = False
                        End Select
                        .YAxisIndex = 0
                    End With
                End If
            End If
            If Not py4 Is Nothing Or Not px4 Is Nothing Then
                If py4.Count > 0 Or px4.Count > 0 Then
                    Dim mycurve As LineItem = Nothing
                    Select Case currcase.datatype
                        Case DataType.Txy, DataType.Pxy, DataType.TPxy
                            mycurve = .AddCurve(y4ctitle, px2.ToArray(GetType(Double)), py4.ToArray(GetType(Double)), Color.Black)
                        Case DataType.Txy, DataType.Pxy
                            mycurve = .AddCurve(y4ctitle, px4.ToArray(GetType(Double)), py1.ToArray(GetType(Double)), Color.Black)
                        Case DataType.TPxx, DataType.Txx, DataType.Pxx
                            mycurve = .AddCurve(y4ctitle, px4.ToArray(GetType(Double)), py4.ToArray(GetType(Double)), Color.Black)
                    End Select
                    With mycurve
                        Dim ppl As ZedGraph.PointPairList = .Points
                        ppl.Sort(ZedGraph.SortType.XValues)
                        Select Case ycurvetypes(3)
                            '1 - somente pontos
                            '2 - pontos e linha
                            '3 - somente linha
                            '4 - linha tracejada
                            '5 - linha tracejada com pontos
                            Case 1
                                .Line.IsVisible = False
                                .Line.IsSmooth = False
                                .Color = Color.LightSalmon
                                .Symbol.Type = ZedGraph.SymbolType.Circle
                                .Symbol.Fill.Type = ZedGraph.FillType.Solid
                                .Symbol.Fill.Color = Color.LightSalmon
                                .Symbol.Fill.IsVisible = True
                                .Symbol.Size = 5
                            Case 2
                                .Line.IsVisible = True
                                .Line.IsSmooth = False
                                .Color = Color.LightSalmon
                                .Symbol.Type = ZedGraph.SymbolType.Circle
                                .Symbol.Fill.Type = ZedGraph.FillType.Solid
                                .Symbol.Fill.Color = Color.LightSalmon
                                .Symbol.Fill.IsVisible = True
                                .Symbol.Size = 5
                            Case 3
                                .Line.IsVisible = True
                                .Line.IsSmooth = True
                                .Color = Color.LightSalmon
                                .Symbol.IsVisible = False
                            Case 4
                                .Line.IsVisible = True
                                .Line.IsSmooth = False
                                .Line.Style = Drawing2D.DashStyle.Dash
                                .Color = Color.LightSalmon
                                .Symbol.IsVisible = False
                            Case 5
                                .Line.IsVisible = True
                                .Line.IsSmooth = False
                                .Line.Style = Drawing2D.DashStyle.Dash
                                .Color = Color.LightSalmon
                                .Symbol.Type = ZedGraph.SymbolType.Circle
                                .Symbol.Fill.Type = ZedGraph.FillType.Solid
                                .Symbol.Fill.Color = Color.LightSalmon
                                .Symbol.Fill.IsVisible = True
                                .Symbol.Size = 5
                            Case 6
                                .Line.IsVisible = True
                                .Line.IsSmooth = False
                                .Color = Color.LightSalmon
                                .Symbol.IsVisible = False
                        End Select
                        .YAxisIndex = 0
                    End With
                End If
            End If

            With .Legend
                .Border.IsVisible = False
                .Position = ZedGraph.LegendPos.BottomCenter
                .IsHStack = True
                .FontSpec.Size = 10
            End With

            With .XAxis
                .Type = AxisType.Log
                .Title.Text = xtitle
                .Title.FontSpec.Size = 11
                .Scale.MinAuto = False
                .Scale.MaxAuto = False
                .Scale.Min = 0.0#
                .Scale.Max = 1.0#
                .Scale.FontSpec.Size = 10
                Select Case xformat
                    Case 1
                        .Type = ZedGraph.AxisType.Linear
                    Case 2
                        .Type = ZedGraph.AxisType.Linear
                    Case 3
                        .Type = ZedGraph.AxisType.DateAsOrdinal
                        .Scale.Format = "dd/MM/yy"
                End Select
            End With

            With .Legend
                .Border.IsVisible = False
                .IsVisible = True
                .Position = ZedGraph.LegendPos.TopCenter
                .FontSpec.Size = 11
            End With

            .Margin.All = 10

            With .Title
                .IsVisible = True
                .Text = title
                .FontSpec.Size = 12
            End With

            Me.graph.IsAntiAlias = True
            Me.graph.AxisChange()
            Me.graph.Invalidate()

        End With

        Select Case Me.currcase.datatype
            Case DataType.Pxy, DataType.Txy, DataType.TPxy
                With graph2.GraphPane
                    .GraphObjList.Clear()
                    .CurveList.Clear()
                    .YAxisList.Clear()
                    If px2.Count > 0 Then
                        Dim ya0 As New ZedGraph.YAxis(y2title)
                        ya0.Scale.FontSpec.Size = 10
                        ya0.Title.FontSpec.Size = 11
                        ya0.Scale.Min = 0.0#
                        ya0.Scale.Max = 1.0#
                        .YAxisList.Add(ya0)
                        Dim mycurve As LineItem = Nothing
                        mycurve = .AddCurve(y5ctitle, px.ToArray(GetType(Double)), px2.ToArray(GetType(Double)), Color.Black)
                        With mycurve
                            Dim ppl As ZedGraph.PointPairList = .Points
                            ppl.Sort(ZedGraph.SortType.XValues)
                            Select Case ycurvetypes(4)
                                '1 - somente pontos
                                '2 - pontos e linha
                                '3 - somente linha
                                '4 - linha tracejada
                                '5 - linha tracejada com pontos
                                Case 1
                                    .Line.IsVisible = False
                                    .Line.IsSmooth = False
                                    .Color = Color.Red
                                    .Symbol.Type = ZedGraph.SymbolType.Circle
                                    .Symbol.Fill.Type = ZedGraph.FillType.Solid
                                    .Symbol.Fill.Color = Color.Red
                                    .Symbol.Fill.IsVisible = True
                                    .Symbol.Size = 5
                                Case 2
                                    .Line.IsVisible = True
                                    .Line.IsSmooth = False
                                    .Color = Color.Red
                                    .Symbol.Type = ZedGraph.SymbolType.Circle
                                    .Symbol.Fill.Type = ZedGraph.FillType.Solid
                                    .Symbol.Fill.Color = Color.Red
                                    .Symbol.Fill.IsVisible = True
                                    .Symbol.Size = 5
                                Case 3
                                    .Line.IsVisible = True
                                    .Line.IsSmooth = True
                                    .Color = Color.Red
                                    .Symbol.IsVisible = False
                                Case 4
                                    .Line.IsVisible = True
                                    .Line.IsSmooth = False
                                    .Line.Style = Drawing2D.DashStyle.Dash
                                    .Color = Color.Red
                                    .Symbol.IsVisible = False
                                Case 5
                                    .Line.IsVisible = True
                                    .Line.IsSmooth = False
                                    .Line.Style = Drawing2D.DashStyle.Dash
                                    .Color = Color.Red
                                    .Symbol.Type = ZedGraph.SymbolType.Circle
                                    .Symbol.Fill.Type = ZedGraph.FillType.Solid
                                    .Symbol.Fill.Color = Color.Red
                                    .Symbol.Fill.IsVisible = True
                                    .Symbol.Size = 5
                                Case 6
                                    .Line.IsVisible = True
                                    .Line.IsSmooth = False
                                    Dim c1 As Color = Color.FromArgb(255, rnd.Next(0, 255), rnd.Next(0, 255), rnd.Next(0, 255))
                                    .Color = Color.Red
                                    .Symbol.IsVisible = False
                            End Select
                            .YAxisIndex = 0
                        End With
                    End If
                    If py5.Count > 0 Then
                        Dim mycurve As LineItem = Nothing
                        mycurve = .AddCurve(y6ctitle, px.ToArray(GetType(Double)), py5.ToArray(GetType(Double)), Color.Black)
                        With mycurve
                            Dim ppl As ZedGraph.PointPairList = .Points
                            ppl.Sort(ZedGraph.SortType.XValues)
                            Select Case ycurvetypes(5)
                                '1 - somente pontos
                                '2 - pontos e linha
                                '3 - somente linha
                                '4 - linha tracejada
                                '5 - linha tracejada com pontos
                                Case 1
                                    .Line.IsVisible = False
                                    .Line.IsSmooth = False
                                    .Color = Color.Red
                                    .Symbol.Type = ZedGraph.SymbolType.Circle
                                    .Symbol.Fill.Type = ZedGraph.FillType.Solid
                                    .Symbol.Fill.Color = Color.Red
                                    .Symbol.Fill.IsVisible = True
                                    .Symbol.Size = 5
                                Case 2
                                    .Line.IsVisible = True
                                    .Line.IsSmooth = False
                                    .Color = Color.Red
                                    .Symbol.Type = ZedGraph.SymbolType.Circle
                                    .Symbol.Fill.Type = ZedGraph.FillType.Solid
                                    .Symbol.Fill.Color = Color.Red
                                    .Symbol.Fill.IsVisible = True
                                    .Symbol.Size = 5
                                Case 3
                                    .Line.IsVisible = True
                                    .Line.IsSmooth = True
                                    .Color = Color.Red
                                    .Symbol.IsVisible = False
                                Case 4
                                    .Line.IsVisible = True
                                    .Line.IsSmooth = False
                                    .Line.Style = Drawing2D.DashStyle.Dash
                                    .Color = Color.Red
                                    .Symbol.IsVisible = False
                                Case 5
                                    .Line.IsVisible = True
                                    .Line.IsSmooth = False
                                    .Line.Style = Drawing2D.DashStyle.Dash
                                    .Color = Color.Red
                                    .Symbol.Type = ZedGraph.SymbolType.Circle
                                    .Symbol.Fill.Type = ZedGraph.FillType.Solid
                                    .Symbol.Fill.Color = Color.Red
                                    .Symbol.Fill.IsVisible = True
                                    .Symbol.Size = 5
                                Case 6
                                    .Line.IsVisible = True
                                    .Line.IsSmooth = False
                                    Dim c1 As Color = Color.FromArgb(255, rnd.Next(0, 255), rnd.Next(0, 255), rnd.Next(0, 255))
                                    .Color = Color.Red
                                    .Symbol.IsVisible = False
                            End Select
                            .YAxisIndex = 0
                        End With
                    End If

                    With .AddCurve("", New Double() {0.0#, 1.0#}, New Double() {0.0#, 1.0#}, Color.Black, SymbolType.None)
                        .Line.IsVisible = True
                        .Line.Width = 1
                        .YAxisIndex = 0
                    End With

                    With .Legend
                        .Border.IsVisible = False
                        .Position = ZedGraph.LegendPos.BottomCenter
                        .IsHStack = True
                        .FontSpec.Size = 10
                    End With

                    With .XAxis
                        .Title.Text = xtitle
                        .Title.FontSpec.Size = 11
                        .Scale.MinAuto = False
                        .Scale.MaxAuto = False
                        .Scale.Min = 0.0#
                        .Scale.Max = 1.0#
                        .Scale.FontSpec.Size = 10
                        Select Case xformat
                            Case 1
                                .Type = ZedGraph.AxisType.Linear
                            Case 2
                                .Type = ZedGraph.AxisType.Linear
                            Case 3
                                .Type = ZedGraph.AxisType.DateAsOrdinal
                                .Scale.Format = "dd/MM/yy"
                        End Select
                    End With

                    With .Legend
                        .Border.IsVisible = False
                        .IsVisible = True
                        .Position = ZedGraph.LegendPos.TopCenter
                        .FontSpec.Size = 11
                    End With

                    .Margin.All = 10

                    With .Title
                        .IsVisible = True
                        .Text = title
                        .FontSpec.Size = 12
                    End With

                End With

                Me.graph2.IsAntiAlias = True
                Me.graph2.AxisChange()
                Me.graph2.Invalidate()

        End Select

    End Sub

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        cancel = True
        Application.DoEvents()
    End Sub

    Public Sub PasteData(ByRef dgv As DataGridView)
        Dim tArr() As String
        Dim arT() As String
        Dim i, ii As Integer
        Dim c, cc, r As Integer

        tArr = Clipboard.GetText().Split(Environment.NewLine)

        If dgv.SelectedCells.Count > 0 Then
            r = dgv.SelectedCells(0).RowIndex
            c = dgv.SelectedCells(0).ColumnIndex
        Else
            r = 0
            c = 0
        End If
        For i = 0 To tArr.Length - 1
            If tArr(i) <> "" Then
                arT = tArr(i).Split(vbTab)
                For ii = 0 To arT.Length - 1
                    If r > dgv.Rows.Count - 1 Then
                        dgv.Rows.Add()
                        dgv.Rows(0).Cells(0).Selected = True
                    End If
                Next
                r = r + 1
            End If
        Next
        If dgv.SelectedCells.Count > 0 Then
            r = dgv.SelectedCells(0).RowIndex
            c = dgv.SelectedCells(0).ColumnIndex
        Else
            r = 0
            c = 0
        End If
        For i = 0 To tArr.Length - 1
            If tArr(i) <> "" Then
                arT = tArr(i).Split(vbTab)
                cc = c
                For ii = 0 To arT.Length - 1
                    cc = GetNextVisibleCol(dgv, cc)
                    If cc > dgv.ColumnCount - 1 Then Exit For
                    dgv.Item(cc, r).Value = arT(ii).TrimStart
                    cc = cc + 1
                Next
                r = r + 1
            End If
        Next

    End Sub

    Private Function GetNextVisibleCol(ByRef dgv As DataGridView, ByVal stidx As Integer) As Integer

        Dim i As Integer

        For i = stidx To dgv.ColumnCount - 1
            If dgv.Columns(i).Visible Then Return i
        Next

        Return Nothing

    End Function

    Private Sub GridExpData_KeyDown1(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles GridExpData.KeyDown

        If e.KeyCode = Keys.Delete And e.Modifiers = Keys.Shift Then
            Dim toremove As New ArrayList
            For Each c As DataGridViewCell In Me.GridExpData.SelectedCells
                If Not toremove.Contains(c.RowIndex) Then toremove.Add(c.RowIndex)
            Next
            For Each i As Integer In toremove
                Me.GridExpData.Rows.RemoveAt(i)
            Next
        ElseIf e.KeyCode = Keys.V And e.Modifiers = Keys.Control Then
            PasteData(GridExpData)
        ElseIf e.KeyCode = Keys.Delete Then
            For Each c As DataGridViewCell In Me.GridExpData.SelectedCells
                c.Value = ""
            Next
        End If

    End Sub

    Private Sub cbModel_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cbModel.SelectedIndexChanged
        Select Case cbModel.SelectedItem.ToString
            Case "PC-SAFT", "Peng-Robinson", "Soave-Redlich-Kwong"
                With gridInEst.Rows
                    .Clear()
                    .Add(New Object() {"kij", 0.0#})
                End With
                Button1.Enabled = False
                Button2.Enabled = False
            Case "Lee-Kesler-Plöcker"
                With gridInEst.Rows
                    .Clear()
                    .Add(New Object() {"kij", 1.0#})
                End With
                Button1.Enabled = False
                Button2.Enabled = False
            Case "PRSV2-M"
                With gridInEst.Rows
                    .Clear()
                    .Add(New Object() {"kij", 0.0#})
                    .Add(New Object() {"kji", 0.0#})
                End With
                Button1.Enabled = False
                Button2.Enabled = False
            Case "PRSV2-VL"
                With gridInEst.Rows
                    .Clear()
                    .Add(New Object() {"kij", 0.001#})
                    .Add(New Object() {"kji", 0.001#})
                End With
                Button1.Enabled = False
                Button2.Enabled = False
            Case "UNIQUAC"
                With gridInEst.Rows
                    .Clear()
                    .Add(New Object() {"A12", 0.0#})
                    .Add(New Object() {"A21", 0.0#})
                End With
                Button1.Enabled = True
                Button2.Enabled = True
            Case "NRTL"
                With gridInEst.Rows
                    .Clear()
                    .Add(New Object() {"A12", 0.0#})
                    .Add(New Object() {"A21", 0.0#})
                    .Add(New Object() {"alpha12", 0.3#})
                End With
                Button1.Enabled = True
                Button2.Enabled = True
        End Select
    End Sub

    Private Sub tbTitle_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tbTitle.TextChanged
        Me.Text = tbTitle.Text
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click

        Select Case cbModel.SelectedItem.ToString
            Case "NRTL"
                If cbDataType.SelectedItem.ToString.Contains("LL") Then
                    Try
                        Dim estimates As Double() = EstimateNRTL(cbCompound1.SelectedItem.ToString, cbCompound2.SelectedItem.ToString, "UNIFAC-LL")
                        Me.gridInEst.Rows(0).Cells(1).Value = estimates(0)
                        Me.gridInEst.Rows(1).Cells(1).Value = estimates(1)
                        Me.gridInEst.Rows(2).Cells(1).Value = estimates(2)
                    Catch ex As Exception
                        MessageBox.Show(ex.ToString, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End Try
                Else
                    Try
                        Dim estimates As Double() = EstimateNRTL(cbCompound1.SelectedItem.ToString, cbCompound2.SelectedItem.ToString, "UNIFAC")
                        Me.gridInEst.Rows(0).Cells(1).Value = estimates(0)
                        Me.gridInEst.Rows(1).Cells(1).Value = estimates(1)
                        Me.gridInEst.Rows(2).Cells(1).Value = estimates(2)
                    Catch ex As Exception
                        MessageBox.Show(ex.ToString, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End Try
                End If
            Case "UNIQUAC"
                If cbDataType.SelectedItem.ToString.Contains("LL") Then
                    Try
                        Dim estimates As Double() = EstimateUNIQUAC(cbCompound1.SelectedItem.ToString, cbCompound2.SelectedItem.ToString, "UNIFAC-LL")
                        Me.gridInEst.Rows(0).Cells(1).Value = estimates(0)
                        Me.gridInEst.Rows(1).Cells(1).Value = estimates(1)
                    Catch ex As Exception
                        MessageBox.Show(ex.ToString, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End Try
                Else
                    Try
                        Dim estimates As Double() = EstimateUNIQUAC(cbCompound1.SelectedItem.ToString, cbCompound2.SelectedItem.ToString, "UNIFAC")
                        Me.gridInEst.Rows(0).Cells(1).Value = estimates(0)
                        Me.gridInEst.Rows(1).Cells(1).Value = estimates(1)
                    Catch ex As Exception
                        MessageBox.Show(ex.ToString, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End Try
                End If
        End Select

    End Sub

    Function EstimateUNIQUAC(ByVal id1 As String, ByVal id2 As String, ByVal model As String) As Double()

        Dim count As Integer = 0
        Dim delta1 As Double = 10
        Dim delta2 As Double = 10

        Dim ppn As New DWSIM.SimulationObjects.PropertyPackages.UNIQUACPropertyPackage(True)
        Dim uniquac As New DWSIM.SimulationObjects.PropertyPackages.Auxiliary.UNIQUAC

        Dim ms As New DWSIM.SimulationObjects.Streams.MaterialStream("", "")

        Dim ppu As Object = Nothing
        Dim unifac As Object = Nothing

        Select Case model
            Case "UNIFAC"
                ppu = New DWSIM.SimulationObjects.PropertyPackages.UNIFACPropertyPackage(True)
                unifac = New DWSIM.SimulationObjects.PropertyPackages.Auxiliary.Unifac
            Case "UNIFAC-LL"
                ppu = New DWSIM.SimulationObjects.PropertyPackages.UNIFACLLPropertyPackage(True)
                unifac = New DWSIM.SimulationObjects.PropertyPackages.Auxiliary.UnifacLL
            Case "MODFAC"
                ppu = New DWSIM.SimulationObjects.PropertyPackages.MODFACPropertyPackage(True)
                unifac = New DWSIM.SimulationObjects.PropertyPackages.Auxiliary.Modfac
        End Select

        Dim comp1, comp2 As ConstantProperties
        comp1 = FormMain.AvailableComponents(id1)
        comp2 = FormMain.AvailableComponents(id2)

        With ms
            For Each phase As DWSIM.ClassesBasicasTermodinamica.Fase In ms.Fases.Values
                With phase
                    .Componentes.Add(comp1.Name, New DWSIM.ClassesBasicasTermodinamica.Substancia(comp1.Name, ""))
                    .Componentes(comp1.Name).ConstantProperties = comp1
                    .Componentes.Add(comp2.Name, New DWSIM.ClassesBasicasTermodinamica.Substancia(comp2.Name, ""))
                    .Componentes(comp2.Name).ConstantProperties = comp2
                End With
            Next
        End With

        ppn.CurrentMaterialStream = ms
        ppu.CurrentMaterialStream = ms

        Dim T1 = 298.15

        Dim actu(1), actn(1), actnd(1), fx(1), fxd(1), dfdx(1, 1), x(1), x0(1), dx(1) As Double

        actu(0) = unifac.GAMMA(T1, New Object() {0.25, 0.75}, ppu.RET_VQ(), ppu.RET_VR, ppu.RET_VEKI, 0)
        actu(1) = unifac.GAMMA(T1, New Object() {0.75, 0.25}, ppu.RET_VQ(), ppu.RET_VR, ppu.RET_VEKI, 0)

        x(0) = gridInEst.Rows(0).Cells(1).Value
        x(1) = gridInEst.Rows(1).Cells(1).Value

        If x(0) = 0 Then x(0) = -100
        If x(1) = 0 Then x(1) = 100

        Do

            uniquac.InteractionParameters.Clear()
            uniquac.InteractionParameters.Add(ppn.RET_VIDS()(0), New Dictionary(Of String, DWSIM.SimulationObjects.PropertyPackages.Auxiliary.UNIQUAC_IPData))
            uniquac.InteractionParameters(ppn.RET_VIDS()(0)).Add(ppn.RET_VIDS()(1), New DWSIM.SimulationObjects.PropertyPackages.Auxiliary.UNIQUAC_IPData())
            uniquac.InteractionParameters(ppn.RET_VIDS()(0))(ppn.RET_VIDS()(1)).A12 = x(0)
            uniquac.InteractionParameters(ppn.RET_VIDS()(0))(ppn.RET_VIDS()(1)).A21 = x(1)

            actnd(0) = uniquac.GAMMA(T1, New Object() {0.25, 0.75}, ppn.RET_VIDS, ppn.RET_VQ, ppn.RET_VR, 0)
            actnd(1) = uniquac.GAMMA(T1, New Object() {0.75, 0.25}, ppn.RET_VIDS, ppn.RET_VQ, ppn.RET_VR, 0)

            fx(0) = Math.Log(actu(0) / actnd(0))
            fx(1) = Math.Log(actu(1) / actnd(1))

            uniquac.InteractionParameters.Clear()
            uniquac.InteractionParameters.Add(ppn.RET_VIDS()(0), New Dictionary(Of String, DWSIM.SimulationObjects.PropertyPackages.Auxiliary.UNIQUAC_IPData))
            uniquac.InteractionParameters(ppn.RET_VIDS()(0)).Add(ppn.RET_VIDS()(1), New DWSIM.SimulationObjects.PropertyPackages.Auxiliary.UNIQUAC_IPData())
            uniquac.InteractionParameters(ppn.RET_VIDS()(0))(ppn.RET_VIDS()(1)).A12 = x(0) + delta1
            uniquac.InteractionParameters(ppn.RET_VIDS()(0))(ppn.RET_VIDS()(1)).A21 = x(1)

            actnd(0) = uniquac.GAMMA(T1, New Object() {0.25, 0.75}, ppn.RET_VIDS, ppn.RET_VQ, ppn.RET_VR, 0)
            actnd(1) = uniquac.GAMMA(T1, New Object() {0.75, 0.25}, ppn.RET_VIDS, ppn.RET_VQ, ppn.RET_VR, 0)

            fxd(0) = Math.Log(actu(0) / actnd(0))
            fxd(1) = Math.Log(actu(1) / actnd(1))

            dfdx(0, 0) = -(fxd(0) - fx(0)) / delta1
            dfdx(1, 0) = -(fxd(1) - fx(1)) / delta1

            uniquac.InteractionParameters.Clear()
            uniquac.InteractionParameters.Add(ppn.RET_VIDS()(0), New Dictionary(Of String, DWSIM.SimulationObjects.PropertyPackages.Auxiliary.UNIQUAC_IPData))
            uniquac.InteractionParameters(ppn.RET_VIDS()(0)).Add(ppn.RET_VIDS()(1), New DWSIM.SimulationObjects.PropertyPackages.Auxiliary.UNIQUAC_IPData())
            uniquac.InteractionParameters(ppn.RET_VIDS()(0))(ppn.RET_VIDS()(1)).A12 = x(0)
            uniquac.InteractionParameters(ppn.RET_VIDS()(0))(ppn.RET_VIDS()(1)).A21 = x(1) + delta2

            actnd(0) = uniquac.GAMMA(T1, New Object() {0.25, 0.75}, ppn.RET_VIDS, ppn.RET_VQ, ppn.RET_VR, 0)
            actnd(1) = uniquac.GAMMA(T1, New Object() {0.75, 0.25}, ppn.RET_VIDS, ppn.RET_VQ, ppn.RET_VR, 0)

            fxd(0) = Math.Log(actu(0) / actnd(0))
            fxd(1) = Math.Log(actu(1) / actnd(1))

            dfdx(0, 1) = -(fxd(0) - fx(0)) / delta2
            dfdx(1, 1) = -(fxd(1) - fx(1)) / delta2

            'solve linear system
            DWSIM.MathEx.SysLin.rsolve.rmatrixsolve(dfdx, fx, UBound(fx) + 1, dx)

            x0(0) = x(0)
            x0(1) = x(1)

            x(0) += dx(0)
            x(1) += dx(1)

            count += 1

        Loop Until Math.Abs(fx(0) + fx(1)) < 0.01 Or count > 500

        If count >= 500 Then
            MessageBox.Show("Parameter estimation through UNIFAC failed: Reached the maximum number of iterations.", "UNIFAC Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End If
        Return New Double() {x(0), x(1)}

    End Function

    Function EstimateNRTL(ByVal id1 As String, ByVal id2 As String, ByVal model As String) As Double()

        Dim count As Integer = 0
        Dim delta1 As Double = 100
        Dim delta2 As Double = 100
        Dim delta3 As Double = 0.1

        Dim ppn As New DWSIM.SimulationObjects.PropertyPackages.NRTLPropertyPackage(True)
        Dim nrtl As New DWSIM.SimulationObjects.PropertyPackages.Auxiliary.NRTL

        Dim ms As New DWSIM.SimulationObjects.Streams.MaterialStream("", "")

        Dim ppu As Object = Nothing
        Dim unifac As Object = Nothing

        Select Case model
            Case "UNIFAC"
                ppu = New DWSIM.SimulationObjects.PropertyPackages.UNIFACPropertyPackage(True)
                unifac = New DWSIM.SimulationObjects.PropertyPackages.Auxiliary.Unifac
            Case "UNIFAC-LL"
                ppu = New DWSIM.SimulationObjects.PropertyPackages.UNIFACLLPropertyPackage(True)
                unifac = New DWSIM.SimulationObjects.PropertyPackages.Auxiliary.UnifacLL
            Case "MODFAC"
                ppu = New DWSIM.SimulationObjects.PropertyPackages.MODFACPropertyPackage(True)
                unifac = New DWSIM.SimulationObjects.PropertyPackages.Auxiliary.Modfac
        End Select

        Dim comp1, comp2 As ConstantProperties
        comp1 = FormMain.AvailableComponents(id1)
        comp2 = FormMain.AvailableComponents(id2)

        With ms
            For Each phase As DWSIM.ClassesBasicasTermodinamica.Fase In ms.Fases.Values
                With phase
                    .Componentes.Add(comp1.Name, New DWSIM.ClassesBasicasTermodinamica.Substancia(comp1.Name, ""))
                    .Componentes(comp1.Name).ConstantProperties = comp1
                    .Componentes.Add(comp2.Name, New DWSIM.ClassesBasicasTermodinamica.Substancia(comp2.Name, ""))
                    .Componentes(comp2.Name).ConstantProperties = comp2
                End With
            Next
        End With

        ppn.CurrentMaterialStream = ms
        ppu.CurrentMaterialStream = ms

        Dim T1 = 298.15

        Dim actu(1), actn(1), actnd(1), fx(1), fxd(1), dfdx(1, 1), x(1), x0(1), dx(1) As Double

        Try
            actu(0) = unifac.GAMMA(T1, New Object() {0.25, 0.75}, ppu.RET_VQ(), ppu.RET_VR, ppu.RET_VEKI, 0)
            actu(1) = unifac.GAMMA(T1, New Object() {0.75, 0.25}, ppu.RET_VQ(), ppu.RET_VR, ppu.RET_VEKI, 0)
        Catch ex As Exception
            MessageBox.Show(ex.ToString, DWSIM.App.GetLocalString("Erro"), MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try

        x(0) = gridInEst.Rows(0).Cells(1).Value
        x(1) = gridInEst.Rows(1).Cells(1).Value

        If x(0) = 0 Then x(0) = 0
        If x(1) = 0 Then x(1) = 0

        Do

            nrtl.InteractionParameters.Clear()
            nrtl.InteractionParameters.Add(ppn.RET_VIDS()(0), New Dictionary(Of String, DWSIM.SimulationObjects.PropertyPackages.Auxiliary.NRTL_IPData))
            nrtl.InteractionParameters(ppn.RET_VIDS()(0)).Add(ppn.RET_VIDS()(1), New DWSIM.SimulationObjects.PropertyPackages.Auxiliary.NRTL_IPData())
            nrtl.InteractionParameters(ppn.RET_VIDS()(0))(ppn.RET_VIDS()(1)).A12 = x(0)
            nrtl.InteractionParameters(ppn.RET_VIDS()(0))(ppn.RET_VIDS()(1)).A21 = x(1)
            nrtl.InteractionParameters(ppn.RET_VIDS()(0))(ppn.RET_VIDS()(1)).alpha12 = 0.3

            actnd(0) = nrtl.GAMMA(T1, New Object() {0.25, 0.75}, ppn.RET_VIDS, 0)
            actnd(1) = nrtl.GAMMA(T1, New Object() {0.75, 0.25}, ppn.RET_VIDS, 0)

            fx(0) = Math.Log(actu(0) / actnd(0))
            fx(1) = Math.Log(actu(1) / actnd(1))

            nrtl.InteractionParameters.Clear()
            nrtl.InteractionParameters.Add(ppn.RET_VIDS()(0), New Dictionary(Of String, DWSIM.SimulationObjects.PropertyPackages.Auxiliary.NRTL_IPData))
            nrtl.InteractionParameters(ppn.RET_VIDS()(0)).Add(ppn.RET_VIDS()(1), New DWSIM.SimulationObjects.PropertyPackages.Auxiliary.NRTL_IPData())
            nrtl.InteractionParameters(ppn.RET_VIDS()(0))(ppn.RET_VIDS()(1)).A12 = x(0) + delta1
            nrtl.InteractionParameters(ppn.RET_VIDS()(0))(ppn.RET_VIDS()(1)).A21 = x(1)
            nrtl.InteractionParameters(ppn.RET_VIDS()(0))(ppn.RET_VIDS()(1)).alpha12 = 0.3

            Try
                actnd(0) = nrtl.GAMMA(T1, New Object() {0.25, 0.75}, ppn.RET_VIDS, 0)
                actnd(1) = nrtl.GAMMA(T1, New Object() {0.75, 0.25}, ppn.RET_VIDS, 0)
            Catch ex As Exception
                MessageBox.Show(ex.ToString, DWSIM.App.GetLocalString("Erro"), MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try

            fxd(0) = Math.Log(actu(0) / actnd(0))
            fxd(1) = Math.Log(actu(1) / actnd(1))

            dfdx(0, 0) = -(fxd(0) - fx(0)) / delta1
            dfdx(1, 0) = -(fxd(1) - fx(1)) / delta1

            nrtl.InteractionParameters.Clear()
            nrtl.InteractionParameters.Add(ppn.RET_VIDS()(0), New Dictionary(Of String, DWSIM.SimulationObjects.PropertyPackages.Auxiliary.NRTL_IPData))
            nrtl.InteractionParameters(ppn.RET_VIDS()(0)).Add(ppn.RET_VIDS()(1), New DWSIM.SimulationObjects.PropertyPackages.Auxiliary.NRTL_IPData())
            nrtl.InteractionParameters(ppn.RET_VIDS()(0))(ppn.RET_VIDS()(1)).A12 = x(0)
            nrtl.InteractionParameters(ppn.RET_VIDS()(0))(ppn.RET_VIDS()(1)).A21 = x(1) + delta2
            nrtl.InteractionParameters(ppn.RET_VIDS()(0))(ppn.RET_VIDS()(1)).alpha12 = 0.3

            Try
                actnd(0) = nrtl.GAMMA(T1, New Object() {0.25, 0.75}, ppn.RET_VIDS, 0)
                actnd(1) = nrtl.GAMMA(T1, New Object() {0.75, 0.25}, ppn.RET_VIDS, 0)
            Catch ex As Exception
                MessageBox.Show(ex.ToString, DWSIM.App.GetLocalString("Erro"), MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try

            fxd(0) = Math.Log(actu(0) / actnd(0))
            fxd(1) = Math.Log(actu(1) / actnd(1))

            dfdx(0, 1) = -(fxd(0) - fx(0)) / delta2
            dfdx(1, 1) = -(fxd(1) - fx(1)) / delta2

            'solve linear system
            DWSIM.MathEx.SysLin.rsolve.rmatrixsolve(dfdx, fx, UBound(fx) + 1, dx)

            x0(0) = x(0)
            x0(1) = x(1)

            x(0) += dx(0)
            x(1) += dx(1)

            count += 1

        Loop Until Math.Abs(fx(0) + fx(1)) < 0.01 Or count > 500

        If count > 500 Then
            MessageBox.Show("Parameter estimation through UNIFAC failed: Reached the maximum number of iterations.", "UNIFAC Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End If
        Return New Double() {x(0), x(1), 0.3#}

    End Function

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click

        Select Case cbModel.SelectedItem.ToString
            Case "NRTL"
                Try
                    Dim estimates As Double() = EstimateNRTL(cbCompound1.SelectedItem.ToString, cbCompound2.SelectedItem.ToString, "MODFAC")
                    Me.gridInEst.Rows(0).Cells(1).Value = estimates(0)
                    Me.gridInEst.Rows(1).Cells(1).Value = estimates(1)
                    Me.gridInEst.Rows(2).Cells(1).Value = estimates(2)
                Catch ex As Exception
                    MessageBox.Show(ex.ToString, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            Case "UNIQUAC"
                Try
                    Dim estimates As Double() = EstimateUNIQUAC(cbCompound1.SelectedItem.ToString, cbCompound2.SelectedItem.ToString, "MODFAC")
                    Me.gridInEst.Rows(0).Cells(1).Value = estimates(0)
                    Me.gridInEst.Rows(1).Cells(1).Value = estimates(1)
                Catch ex As Exception
                    MessageBox.Show(ex.ToString, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
        End Select

    End Sub

    Private Sub ToolStripMenuItem1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripMenuItem1.Click, ToolStripMenuItem2.Click, _
    ToolStripMenuItem3.Click, ToolStripMenuItem4.Click, ToolStripMenuItem5.Click, ToolStripMenuItem6.Click, ToolStripMenuItem7.Click

        Dim P, T, x11, x12, y1 As Double, comp1, comp2, model As String
        comp1 = Me.cbCompound1.SelectedItem.ToString
        comp2 = Me.cbCompound2.SelectedItem.ToString

        If CType(sender, ToolStripMenuItem).Name = "ToolStripMenuItem1" Then
            model = "UNIFAC"
        ElseIf CType(sender, ToolStripMenuItem).Name = "ToolStripMenuItem2" Then
            model = "NRTL"
        ElseIf CType(sender, ToolStripMenuItem).Name = "ToolStripMenuItem3" Then
            model = "UNIQUAC"
        ElseIf CType(sender, ToolStripMenuItem).Name = "ToolStripMenuItem4" Then
            model = "Peng-Robinson (PR)"
        ElseIf CType(sender, ToolStripMenuItem).Name = "ToolStripMenuItem5" Then
            model = "Soave-Redlich-Kwong (SRK)"
        ElseIf CType(sender, ToolStripMenuItem).Name = "ToolStripMenuItem6" Then
            model = "Peng-Robinson-Stryjek-Vera 2 (PRSV2-M)"
        ElseIf CType(sender, ToolStripMenuItem).Name = "ToolStripMenuItem7" Then
            model = "Peng-Robinson-Stryjek-Vera 2 (PRSV2-VL)"
        Else
            model = ""
        End If

        For Each c As DataGridViewCell In Me.GridExpData.SelectedCells

            If Me.GridExpData.Rows(c.RowIndex).Cells("colP").Value IsNot Nothing Then
                If Me.GridExpData.Rows(c.RowIndex).Cells("colP").Value.ToString() <> "" Then
                    P = cv.ConverterParaSI(Me.cbPunit.SelectedItem.ToString(), Me.GridExpData.Rows(c.RowIndex).Cells("colP").Value)
                Else
                    P = 0
                End If
            Else
                P = 0
            End If
            If Me.GridExpData.Rows(c.RowIndex).Cells("colT").Value IsNot Nothing Then
                If Me.GridExpData.Rows(c.RowIndex).Cells("colT").Value.ToString() <> "" Then
                    T = cv.ConverterParaSI(Me.cbTunit.SelectedItem.ToString(), Me.GridExpData.Rows(c.RowIndex).Cells("colT").Value)
                Else
                    T = 0
                End If
            Else
                T = 0
            End If
            If Me.GridExpData.Rows(c.RowIndex).Cells("colx1").Value IsNot Nothing Then
                If Me.GridExpData.Rows(c.RowIndex).Cells("colx1").Value.ToString() <> "" Then
                    x11 = Double.Parse(Me.GridExpData.Rows(c.RowIndex).Cells("colx1").Value)
                Else
                    x11 = -1
                End If
            Else
                x11 = -1
            End If
            If Me.GridExpData.Rows(c.RowIndex).Cells("colx2").Value IsNot Nothing Then
                If Me.GridExpData.Rows(c.RowIndex).Cells("colx2").Value.ToString() <> "" Then
                    x12 = Double.Parse(Me.GridExpData.Rows(c.RowIndex).Cells("colx2").Value)
                Else
                    x12 = -1
                End If
            Else
                x12 = -1
            End If
            If Me.GridExpData.Rows(c.RowIndex).Cells("coly1").Value IsNot Nothing Then
                If Me.GridExpData.Rows(c.RowIndex).Cells("coly1").Value.ToString() <> "" Then
                    y1 = Double.Parse(Me.GridExpData.Rows(c.RowIndex).Cells("coly1").Value)
                Else
                    y1 = -1
                End If
            Else
                y1 = -1
            End If

            Dim result As Object = Nothing

            Try

                Select Case c.ColumnIndex
                    Case 0
                        If P = 0.0# Then
                            'T-y => x, P
                            result = Interfaces.ExcelIntegration.TVFFlash(model, 1, T, 1, New Object() {comp1, comp2}, New Double() {y1, 1 - y1}, Nothing, Nothing, Nothing, Nothing)
                            P = result(4, 0)
                            x11 = result(2, 1)
                            Me.GridExpData.Rows(c.RowIndex).Cells("colP").Value = Format(cv.ConverterDoSI(Me.cbPunit.SelectedItem.ToString, P), "N4")
                            Me.GridExpData.Rows(c.RowIndex).Cells("colx1").Value = Format(x11, "N4")
                        ElseIf T = 0.0# Then
                            'P-y => x, T
                            result = Interfaces.ExcelIntegration.PVFFlash(model, 1, P, 1, New Object() {comp1, comp2}, New Double() {y1, 1 - y1}, Nothing, Nothing, Nothing, Nothing)
                            T = result(4, 0)
                            x11 = result(2, 1)
                            Me.GridExpData.Rows(c.RowIndex).Cells("colT").Value = Format(cv.ConverterDoSI(Me.cbTunit.SelectedItem.ToString, T), "N4")
                            Me.GridExpData.Rows(c.RowIndex).Cells("colx1").Value = Format(x11, "N4")
                        End If
                    Case 2
                        If P = 0.0# Then
                            'T-x => y, P
                            result = Interfaces.ExcelIntegration.TVFFlash(model, 1, T, 0, New Object() {comp1, comp2}, New Double() {x11, 1 - x11}, Nothing, Nothing, Nothing, Nothing)
                            P = result(4, 0)
                            y1 = result(2, 0)
                            Me.GridExpData.Rows(c.RowIndex).Cells("colP").Value = Format(cv.ConverterDoSI(Me.cbPunit.SelectedItem.ToString, P), "N4")
                            Me.GridExpData.Rows(c.RowIndex).Cells("coly1").Value = Format(y1, "N4")
                        ElseIf T = 0.0# Then
                            'P-x => y, T
                            result = Interfaces.ExcelIntegration.PVFFlash(model, 1, P, 0, New Object() {comp1, comp2}, New Double() {x11, 1 - x11}, Nothing, Nothing, Nothing, Nothing)
                            T = result(4, 0)
                            y1 = result(2, 0)
                            Me.GridExpData.Rows(c.RowIndex).Cells("colT").Value = Format(cv.ConverterDoSI(Me.cbTunit.SelectedItem.ToString, T), "N4")
                            Me.GridExpData.Rows(c.RowIndex).Cells("coly1").Value = Format(y1, "N4")
                        End If
                    Case 3
                        If y1 = -1 Then
                            'P-x => y, T
                            result = Interfaces.ExcelIntegration.PVFFlash(model, 1, P, 0, New Object() {comp1, comp2}, New Double() {x11, 1 - x11}, Nothing, Nothing, Nothing, Nothing)
                            T = result(4, 0)
                            y1 = result(2, 0)
                            Me.GridExpData.Rows(c.RowIndex).Cells("colT").Value = Format(cv.ConverterDoSI(Me.cbTunit.SelectedItem.ToString, T), "N4")
                            Me.GridExpData.Rows(c.RowIndex).Cells("coly1").Value = Format(y1, "N4")
                        ElseIf x11 = -1 Then
                            'P-y => x, T
                            result = Interfaces.ExcelIntegration.PVFFlash(model, 1, P, 1, New Object() {comp1, comp2}, New Double() {y1, 1 - y1}, Nothing, Nothing, Nothing, Nothing)
                            P = result(4, 0)
                            x11 = result(2, 1)
                            Me.GridExpData.Rows(c.RowIndex).Cells("colT").Value = Format(cv.ConverterDoSI(Me.cbTunit.SelectedItem.ToString, T), "N4")
                            Me.GridExpData.Rows(c.RowIndex).Cells("colx1").Value = Format(x11, "N4")
                        End If
                    Case 4
                        If y1 = -1 Then
                            'T-x => y, P
                            result = Interfaces.ExcelIntegration.TVFFlash(model, 1, T, 0, New Object() {comp1, comp2}, New Double() {x11, 1 - x11}, Nothing, Nothing, Nothing, Nothing)
                            P = result(4, 0)
                            y1 = result(2, 0)
                            Me.GridExpData.Rows(c.RowIndex).Cells("colP").Value = Format(cv.ConverterDoSI(Me.cbPunit.SelectedItem.ToString, P), "N4")
                            Me.GridExpData.Rows(c.RowIndex).Cells("coly1").Value = Format(y1, "N4")
                        ElseIf x11 = -1 Then
                            'T-y => x, P
                            result = Interfaces.ExcelIntegration.TVFFlash(model, 1, T, 1, New Object() {comp1, comp2}, New Double() {y1, 1 - y1}, Nothing, Nothing, Nothing, Nothing)
                            P = result(4, 0)
                            x11 = result(2, 1)
                            Me.GridExpData.Rows(c.RowIndex).Cells("colP").Value = Format(cv.ConverterDoSI(Me.cbPunit.SelectedItem.ToString, P), "N4")
                            Me.GridExpData.Rows(c.RowIndex).Cells("colx1").Value = Format(x11, "N4")
                        End If
                End Select

            Catch ex As Exception
                c.Value = "*"
            End Try

        Next

    End Sub

End Class

Namespace DWSIM.Optimization.DatRegression

    <System.Serializable()> Public Class RegressionCase

        Public comp1, comp2, comp3 As String
        Public filename As String = ""
        Public model As String = "Peng-Robinson"
        Public datatype As DataType = datatype.Pxy
        Public tp, x1p, x2p, yp, pp, calct, calcp, calcy, calcx1l1, calcx1l2 As New ArrayList
        Public method As String = "IPOPT"
        Public objfunction As String = "Least Squares (min T/P)"
        Public includesd As Boolean = False
        Public results As String = ""
        Public advsettings As Object = Nothing
        Public tunit As String = "C"
        Public punit As String = "bar"
        Public cunit As String = ""
        Public tolerance As Double = 0.00001
        Public maxits As Double = 250
        Public iepar1 As Double = 0.0#
        Public iepar2 As Double = 0.0#
        Public iepar3 As Double = 0.0#
        Public title As String = ""
        Public description As String = ""

    End Class

    Public Enum DataType
        Txy = 0
        Pxy = 1
        TPxy = 2
        Txx = 3
        Pxx = 4
        TPxx = 5
    End Enum

End Namespace

