﻿'    Copyright 2008 Daniel Wagner O. de Medeiros
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

Imports System.Xml.Serialization
Imports DWSIM.DWSIM.ClassesBasicasTermodinamica
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.Runtime.Serialization.Formatters
Imports System.IO
Imports Cudafy
Imports Cudafy.Host

Public Class FormOptions

    Inherits System.Windows.Forms.Form

    Private loaded As Boolean = False

    Private Sub FormOptions_FormClosed(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed
        My.Settings.Save()
    End Sub

    Private Sub FormOptions_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        Me.cbGPU.Items.Clear()

        CudafyModes.Target = eGPUType.Emulator
        For Each prop As GPGPUProperties In CudafyHost.GetDeviceProperties(CudafyModes.Target, False)
            Me.cbGPU.Items.Add("Emulator | " & prop.Name & " (" & prop.DeviceId & ")")
        Next
        Try
            CudafyModes.Target = eGPUType.Cuda
            For Each prop As GPGPUProperties In CudafyHost.GetDeviceProperties(CudafyModes.Target, False)
                Me.cbGPU.Items.Add("CUDA | " & prop.Name & " (" & prop.DeviceId & ")")
            Next
        Catch ex As Exception

        End Try
        Try
            CudafyModes.Target = eGPUType.OpenCL
            For Each prop As GPGPUProperties In CudafyHost.GetDeviceProperties(CudafyModes.Target, False)
                Me.cbGPU.Items.Add("OpenCL | " & prop.Name & " (" & prop.DeviceId & ")")
            Next
        Catch ex As Exception

        End Try

        CudafyModes.Target = My.Settings.CudafyTarget

        Dim i As Integer = 0
        Me.cbParallelism.Items.Clear()
        Me.cbParallelism.Items.Add("Default")
        For i = 1 To System.Environment.ProcessorCount
            Me.cbParallelism.Items.Add(i.ToString)
        Next
        If My.Settings.MaxDegreeOfParallelism = -1 Then
            Me.cbParallelism.SelectedIndex = 0
        ElseIf My.Settings.MaxDegreeOfParallelism <= System.Environment.ProcessorCount Then
            Me.cbParallelism.SelectedItem = My.Settings.MaxDegreeOfParallelism.ToString
        Else
            Me.cbParallelism.SelectedIndex = Me.cbParallelism.Items.Count - 1
        End If

        Me.chkEnableParallelCalcs.Checked = My.Settings.EnableParallelProcessing
        Me.chkEnableGPUProcessing.Checked = My.Settings.EnableGPUProcessing
        Me.cbGPU.Enabled = Me.chkEnableGPUProcessing.Checked
        Me.tbGPUCaps.Enabled = Me.chkEnableGPUProcessing.Checked
        Me.cbParallelism.Enabled = Me.chkEnableParallelCalcs.Checked

        If My.Settings.SelectedGPU <> "" Then
            For Each s As String In Me.cbGPU.Items
                If s = My.Settings.SelectedGPU Then
                    Me.cbGPU.SelectedItem = s
                    Exit For
                End If
            Next
        Else
            If Me.cbGPU.Items.Count > 0 Then Me.cbGPU.SelectedIndex = 0
        End If

        Me.KryptonCheckBox1.Checked = My.Settings.ShowTips

        Me.KryptonCheckBox6.Checked = My.Settings.BackupActivated
        Me.KryptonTextBox1.Text = My.Settings.BackupFolder
        Me.TrackBar1.Value = My.Settings.BackupInterval

        Me.chkconsole.Checked = My.Settings.RedirectOutput

        Me.chkUpdates.Checked = My.Settings.CheckForUpdates

        Me.cbudb.Checked = My.Settings.ReplaceComps

        If TrackBar1.Value = 1 Then
            Me.KryptonLabel3.Text = DWSIM.App.GetLocalString("minuto1")
        Else
            Me.KryptonLabel3.Text = TrackBar1.Value & " " & DWSIM.App.GetLocalString("minutos")
        End If

        'databases
        Me.PopulateDBGrid()

        'script paths
        For Each s As String In My.Settings.ScriptPaths
            Me.lbpaths.Items.Add(s)
        Next

        loaded = True

    End Sub

    Public Sub GetCUDACaps(prop As GPGPUProperties)

        Dim i As Integer = 0

        Me.tbGPUCaps.Text = ""

        Me.tbGPUCaps.AppendText(String.Format("   --- General Information for device {0} ---", i) & vbCrLf)
        Me.tbGPUCaps.AppendText(String.Format("Name:  {0}", prop.Name) & vbCrLf)
        Me.tbGPUCaps.AppendText(String.Format("Device Id:  {0}", prop.DeviceId) & vbCrLf)
        Me.tbGPUCaps.AppendText(String.Format("Compute capability:  {0}.{1}", prop.Capability.Major, prop.Capability.Minor) & vbCrLf)
        Me.tbGPUCaps.AppendText(String.Format("Clock rate: {0}", prop.ClockRate) & vbCrLf)
        Me.tbGPUCaps.AppendText(String.Format("Simulated: {0}", prop.IsSimulated) & vbCrLf)

        Me.tbGPUCaps.AppendText(String.Format("   --- Memory Information for device {0} ---", i) & vbCrLf)
        Me.tbGPUCaps.AppendText(String.Format("Total global mem:  {0}", prop.TotalMemory) & vbCrLf)
        Me.tbGPUCaps.AppendText(String.Format("Total constant Mem:  {0}", prop.TotalConstantMemory) & vbCrLf)
        Me.tbGPUCaps.AppendText(String.Format("Max mem pitch:  {0}", prop.MemoryPitch) & vbCrLf)
        Me.tbGPUCaps.AppendText(String.Format("Texture Alignment:  {0}", prop.TextureAlignment) & vbCrLf)

        Me.tbGPUCaps.AppendText(String.Format("   --- MP Information for device {0} ---", i) & vbCrLf)
        Me.tbGPUCaps.AppendText(String.Format("Shared mem per mp: {0}", prop.SharedMemoryPerBlock) & vbCrLf)
        Me.tbGPUCaps.AppendText(String.Format("Registers per mp:  {0}", prop.RegistersPerBlock) & vbCrLf)
        Me.tbGPUCaps.AppendText(String.Format("Threads in warp:  {0}", prop.WarpSize) & vbCrLf)
        Me.tbGPUCaps.AppendText(String.Format("Max threads per block:  {0}", prop.MaxThreadsPerBlock) & vbCrLf)
        Me.tbGPUCaps.AppendText(String.Format("Max thread dimensions:  ({0}, {1}, {2})", prop.MaxThreadsSize.x, prop.MaxThreadsSize.y, prop.MaxThreadsSize.z) & vbCrLf)
        Me.tbGPUCaps.AppendText(String.Format("Max grid dimensions:  ({0}, {1}, {2})", prop.MaxGridSize.x, prop.MaxGridSize.y, prop.MaxGridSize.z) & vbCrLf)

        Me.tbGPUCaps.SelectionStart = 0
        Me.tbGPUCaps.SelectionLength = 0
        Me.tbGPUCaps.ScrollToCaret()

    End Sub

    Private Sub KryptonCheckBox1_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles KryptonCheckBox1.CheckedChanged
        My.Settings.ShowTips = Me.KryptonCheckBox1.Checked
    End Sub

    Private Sub KryptonCheckBox6_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles KryptonCheckBox6.CheckedChanged
        My.Settings.BackupActivated = Me.KryptonCheckBox6.Checked
        If Me.KryptonCheckBox6.Checked Then
            FormMain.TimerBackup.Enabled = True
            Me.KryptonButton1.Enabled = True
            Me.KryptonTextBox1.Enabled = True
            Me.TrackBar1.Enabled = True
        Else
            FormMain.TimerBackup.Enabled = False
            Me.KryptonButton1.Enabled = False
            Me.KryptonTextBox1.Enabled = False
            Me.TrackBar1.Enabled = False
        End If
    End Sub

    Private Sub KryptonButton1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles KryptonButton1.Click
        FolderBrowserDialog1.SelectedPath = KryptonTextBox1.Text
        If FolderBrowserDialog1.ShowDialog() = DialogResult.OK Then
            KryptonTextBox1.Text = FolderBrowserDialog1.SelectedPath
            My.Settings.BackupFolder = KryptonTextBox1.Text
        End If
    End Sub

    Private Sub TrackBar1_Scroll(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TrackBar1.Scroll
        If TrackBar1.Value = 1 Then
            Me.KryptonLabel3.Text = DWSIM.App.GetLocalString("minuto1")
        Else
            Me.KryptonLabel3.Text = TrackBar1.Value & " " & DWSIM.App.GetLocalString("minutos")
        End If
        My.Settings.BackupInterval = TrackBar1.Value
        FormMain.TimerBackup.Interval = TrackBar1.Value * 60000
    End Sub

    Private Sub Button7_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button7.Click
        If Me.OpenFileDialog1.ShowDialog() = Windows.Forms.DialogResult.OK Then
            Dim path = Me.OpenFileDialog1.FileName
            Try
                Dim componentes As ConstantProperties()
                componentes = DWSIM.Databases.UserDB.Read(path)
                If componentes.Length > 0 Then
                    If Me.cbudb.Checked Then
                        For Each c As ConstantProperties In componentes
                            If Not FormMain.AvailableComponents.ContainsKey(c.Name) Then
                                FormMain.AvailableComponents.Add(c.Name, c)
                            Else
                                FormMain.AvailableComponents(c.Name) = c
                            End If
                        Next
                    Else
                        For Each c As ConstantProperties In componentes
                            If Not FormMain.AvailableComponents.ContainsKey(c.Name) Then
                                FormMain.AvailableComponents.Add(c.Name, c)
                            End If
                        Next
                    End If
                    Me.AddDatabase("User" & CStr(My.Settings.UserDatabases.Count + 1), path)
                    MessageBox.Show(DWSIM.App.GetLocalString("UDBAdded"))
                End If
            Catch ex As System.Runtime.Serialization.SerializationException
                MessageBox.Show(DWSIM.App.GetLocalString("OarquivoXMLinformado") & vbCrLf & ex.Message, DWSIM.App.GetLocalString("ErroaoleroarquivoXML"), MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
            End Try
        End If
    End Sub

    Private Sub Button10_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

    Private Sub Button11_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button11.Click
        With Me.ofdcs
            If .ShowDialog = Windows.Forms.DialogResult.OK Then
                Dim filename As String = .FileName
                FormMain.LoadCSDB(filename)
                'chemsep database
                If FormMain.loadedCSDB Then
                    My.Settings.ChemSepDatabasePath = filename
                    Dim name, path2 As String
                    name = "ChemSep"
                    path2 = My.Settings.ChemSepDatabasePath
                    Me.dgvdb.Rows.Add(New Object() {dgvdb.Rows.Count + 1, My.Resources.information, name, path2, DWSIM.App.GetLocalString("Remove")})
                    Me.dgvdb.Rows(Me.dgvdb.Rows.Count - 1).Cells(4).ReadOnly = True
                    For Each r As DataGridViewRow In Me.dgvdb.Rows
                        r.Height = "40"
                    Next
                End If
            End If
        End With
    End Sub

    Sub AddDatabase(ByVal name As String, ByVal path As String)
        If Not My.Settings.UserDatabases.Contains(path) Then
            My.Settings.UserDatabases.Add(path)
            My.Settings.Save()
            With Me.dgvdb.Rows
                .Add(New Object() {dgvdb.Rows.Count + 1, My.Resources.information, name, path, DWSIM.App.GetLocalString("Remove")})
            End With
            For Each r As DataGridViewRow In Me.dgvdb.Rows
                r.Height = "40"
            Next
        End If
    End Sub

    Sub PopulateDBGrid()

        Dim pathsep As Char = Path.DirectorySeparatorChar

        Me.dgvdb.Rows.Clear()
        Dim name, path2 As String

        'dwsim databases
        name = "DWSIM"
        path2 = My.Application.Info.DirectoryPath & pathsep & "data" & pathsep & "databases" & pathsep & "dwsim.xml"
        If File.Exists(path2) Then Me.dgvdb.Rows.Add(New Object() {dgvdb.Rows.Count + 1, My.Resources.information, name, path2, DWSIM.App.GetLocalString("Remove")})
        Me.dgvdb.Rows(Me.dgvdb.Rows.Count - 1).ReadOnly = True
        'name = "Biodiesel"
        'path2 = My.Application.Info.DirectoryPath & pathsep & "data" & pathsep & "databases" & pathsep & "biod_db.xml"
        'If File.Exists(path2) Then Me.dgvdb.Rows.Add(New Object() {dgvdb.Rows.Count + 1, My.Resources.information, name, path2, DWSIM.App.GetLocalString("Remove")})
        'Me.dgvdb.Rows(Me.dgvdb.Rows.Count - 1).ReadOnly = True
        'name = "CheResources"
        'path2 = My.Application.Info.DirectoryPath & pathsep & "data" & pathsep & "databases" & pathsep & "cheres.xml"
        'If File.Exists(path2) Then Me.dgvdb.Rows.Add(New Object() {dgvdb.Rows.Count + 1, My.Resources.information, name, path2, DWSIM.App.GetLocalString("Remove")})
        'Me.dgvdb.Rows(Me.dgvdb.Rows.Count - 1).ReadOnly = True

        'chemsep database
        If FormMain.loadedCSDB Then
            name = "ChemSep"
            path2 = My.Settings.ChemSepDatabasePath
            Me.dgvdb.Rows.Add(New Object() {dgvdb.Rows.Count + 1, My.Resources.information, name, path2, DWSIM.App.GetLocalString("Remove")})
            Me.dgvdb.Rows(Me.dgvdb.Rows.Count - 1).Cells(4).ReadOnly = True
        End If

        Dim i As Integer = 1

        'user databases
        If Not My.Settings.UserDatabases Is Nothing Then
            For Each str As String In My.Settings.UserDatabases
                path2 = str
                If File.Exists(path2) Then Me.dgvdb.Rows.Add(New Object() {dgvdb.Rows.Count + 1, My.Resources.information, "User" & CStr(i), path2, DWSIM.App.GetLocalString("Remove")})
                i = i + 1
            Next
        End If

        For Each r As DataGridViewRow In Me.dgvdb.Rows
            r.Height = "40"
        Next

    End Sub

    Private Sub dgvdb_CellContentClick(ByVal sender As System.Object, ByVal e As System.Windows.Forms.DataGridViewCellEventArgs) Handles dgvdb.CellContentClick

        If e.ColumnIndex = 4 And e.RowIndex > 0 Then

            If Me.dgvdb.Rows(e.RowIndex).Cells(2).Value = "ChemSep" Then

                'remove chemsep database
                My.Settings.ChemSepDatabasePath = ""
                Me.dgvdb.Rows.RemoveAt(e.RowIndex)
                MessageBox.Show(DWSIM.App.GetLocalString("NextStartupOnly"))

            Else

                'remove user database
                My.Settings.UserDatabases.Remove(Me.dgvdb.Rows(e.RowIndex).Cells(2).Value)
                Me.dgvdb.Rows.RemoveAt(e.RowIndex)
                MessageBox.Show(DWSIM.App.GetLocalString("UDBRemoved"))

            End If


        End If

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        If Me.ListView1.SelectedIndices.Count > 0 Then
            Select Case Me.ListView1.SelectedIndices.Item(0)
                Case 0
                    My.Settings.CultureInfo = "pt-BR"
                Case 1
                    My.Settings.CultureInfo = "en-US"
                Case 2
                    My.Settings.CultureInfo = "es"
                Case 3
                    My.Settings.CultureInfo = "de"
            End Select
            My.Settings.Save()
            My.Application.ChangeUICulture(My.Settings.CultureInfo)
            MessageBox.Show(DWSIM.App.GetLocalString("NextStartupOnly"))
        End If
    End Sub

    Private Sub cbudb_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cbudb.CheckedChanged
        My.Settings.ReplaceComps = cbudb.Checked
    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        If MessageBox.Show(DWSIM.App.GetLocalString("AreYouSure"), "DWSIM", MessageBoxButtons.YesNo) = Windows.Forms.DialogResult.Yes Then
            If Directory.Exists(My.Settings.BackupFolder) Then
                For Each f As String In Directory.GetFiles(My.Settings.BackupFolder, "*.dw*")
                    File.Delete(f)
                Next
            End If
        End If
    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        With Me.ofdcs
            If .ShowDialog = Windows.Forms.DialogResult.OK Then
                Dim filename As String = .FileName
                FormMain.LoadCSDB(filename)
                'chemsep database
                If FormMain.loadedCSDB Then
                    My.Settings.ChemSepDatabasePath = filename
                    Dim name, path2 As String
                    name = "ChemSep"
                    path2 = My.Settings.ChemSepDatabasePath
                    Me.dgvdb.Rows.Add(New Object() {dgvdb.Rows.Count + 1, My.Resources.information, name, path2, DWSIM.App.GetLocalString("Remove")})
                    Me.dgvdb.Rows(Me.dgvdb.Rows.Count - 1).Cells(4).ReadOnly = True
                    For Each r As DataGridViewRow In Me.dgvdb.Rows
                        r.Height = "40"
                    Next
                End If
                MessageBox.Show(DWSIM.App.GetLocalString("NextStartupOnly"))
            End If
        End With
    End Sub

    Private Sub btnaddpath_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnaddpath.Click
        My.Settings.ScriptPaths.Add(Me.tbaddpath.Text)
        Me.lbpaths.Items.Add(Me.tbaddpath.Text)
        Me.tbaddpath.Text = ""
    End Sub

    Private Sub btnrmpath_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnrmpath.Click
        If Me.lbpaths.SelectedItems.Count > 0 Then
            My.Settings.ScriptPaths.Remove(Me.lbpaths.SelectedItem)
            Me.lbpaths.Items.Remove(Me.lbpaths.SelectedItem)
        End If
    End Sub

    Private Sub chkconsole_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkconsole.CheckedChanged
        My.Settings.RedirectOutput = chkconsole.Checked
    End Sub

    Private Sub chkUpdates_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkUpdates.CheckedChanged
        My.Settings.CheckForUpdates = Me.chkUpdates.Checked
    End Sub

    Private Sub chkEnableParallelCalcs_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles chkEnableParallelCalcs.CheckedChanged
        My.Settings.EnableParallelProcessing = Me.chkEnableParallelCalcs.Checked
        Me.cbParallelism.Enabled = Me.chkEnableParallelCalcs.Checked
    End Sub

    Private Sub cbParallelism_SelectedIndexChanged(sender As System.Object, e As System.EventArgs) Handles cbParallelism.SelectedIndexChanged
        If Me.cbParallelism.SelectedIndex = 0 Then
            My.Settings.MaxDegreeOfParallelism = -1
        Else
            My.Settings.MaxDegreeOfParallelism = Me.cbParallelism.SelectedItem
        End If
    End Sub

    Private Sub cbGPU_SelectedIndexChanged(sender As System.Object, e As System.EventArgs) Handles cbGPU.SelectedIndexChanged
        If loaded Then
            If cbGPU.SelectedItem.ToString.Contains("Emulator") Then
                My.Settings.CudafyTarget = eGPUType.Emulator
            ElseIf cbGPU.SelectedItem.ToString.Contains("CUDA") Then
                My.Settings.CudafyTarget = eGPUType.Cuda
            Else
                My.Settings.CudafyTarget = eGPUType.OpenCL
            End If
            For Each prop As GPGPUProperties In CudafyHost.GetDeviceProperties(CudafyModes.Target, False)
                If Me.cbGPU.SelectedItem.ToString.Split("|")(1).Contains(prop.Name) Then
                    My.Settings.SelectedGPU = Me.cbGPU.SelectedItem.ToString
                    My.Settings.CudafyDeviceID = prop.DeviceId
                    GetCUDACaps(prop)
                    Exit For
                End If
            Next
            MessageBox.Show(DWSIM.App.GetLocalString("NextStartupOnly"))
        End If
    End Sub

    Private Sub chkEnableGPUProcessing_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles chkEnableGPUProcessing.CheckedChanged
        Me.cbGPU.Enabled = chkEnableGPUProcessing.Checked
        Me.tbGPUCaps.Enabled = chkEnableGPUProcessing.Checked
        My.Settings.EnableGPUProcessing = chkEnableGPUProcessing.Checked
    End Sub
End Class