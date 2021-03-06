﻿Imports System.Net
Imports System.Runtime.InteropServices

Public Class Main

    ' url to the memory address config file
    Const MEMORY_ADDRESSES_CONFIG_URL = "https://raw.githubusercontent.com/SG57/FFXIV-Camera-Settings/master/Memory-Addresses.txt"



    Dim _memory As Memory = New Memory
    Dim _shouldSaveSettings As Boolean = False





    Private Sub AboutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AboutToolStripMenuItem.Click
        MsgBox("A simple program that will adjust the camera settings for FFXIV, such as zoom and field of view." & vbCrLf & vbCrLf & vbCrLf & "Copyright ©  2014" & vbCrLf & "Cord Rehn <jordansg57@gmail.com>", MsgBoxStyle.Information, "About FFXIV Camera Settings")
    End Sub

    Private Sub DonateToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles DonateToolStripMenuItem.Click
        System.Diagnostics.Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=JordanSg57%40gmail%2ecom&lc=US&item_name=Freelance%20Developer&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donateCC_LG%2egif%3aNonHosted")
    End Sub





    Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.sliderZoomMax.Value = My.Settings.ZoomMax * 100
        Me.sliderFov.Value = My.Settings.FOV * 100
        Me.checkSetZoom.Checked = My.Settings.SetZoomCurrent

        _shouldSaveSettings = True

        refreshProcessComboBoxItems()
        refreshFovLabel()
        refreshZoomMaxLabel()
    End Sub





    Private Sub refreshFovLabel()
        Me.lblFov.Text = (Me.sliderFov.Value / 100.0).ToString("N2")
    End Sub

    Private Sub refreshZoomMaxLabel()
        Me.lblZoomMax.Text = (Me.sliderZoomMax.Value / 100.0).ToString("N2")
    End Sub

    Private Sub refreshProcessComboBoxItems()
        Dim old_selected_item = Me.comboProcesses.SelectedItem
        Me.comboProcesses.Items.Clear()

        Dim ffxiv_processes As Process() = Memory.GetFFXivProcesses()
        For Each proc In ffxiv_processes
            Me.comboProcesses.Items.Add(proc.Id)
        Next

        If Not IsNothing(old_selected_item) Then
            If Me.comboProcesses.Items.Contains(old_selected_item) Then
                Me.comboProcesses.SelectedItem = old_selected_item
            End If
        Else
            If ffxiv_processes.Length > 0 Then
                Me.comboProcesses.SelectedIndex = 0
            Else
                Me.comboProcesses.SelectedItem = Nothing
            End If
        End If

        comboProcesses_SelectionChangeCommitted(Nothing, Nothing)
    End Sub





    Private Sub ComboBox1_DropDown(sender As Object, e As EventArgs) Handles comboProcesses.DropDown
        refreshProcessComboBoxItems()
    End Sub

    Private Sub comboProcesses_SelectionChangeCommitted(sender As Object, e As EventArgs) Handles comboProcesses.SelectionChangeCommitted
        Me.groupZoom.Visible = False
        Me.groupFov.Visible = False

        If IsNothing(Me.comboProcesses.SelectedItem) Then Return

        If Not _memory.AttachToProcess(Me.comboProcesses.SelectedItem) Then
            MsgBox("Failed to attach to FFXIV process ID: " & Me.comboProcesses.SelectedItem, MsgBoxStyle.Critical, "Process Error")
            Me.comboProcesses.SelectedItem = Nothing
        Else
            Me.groupZoom.Visible = True
            Me.groupFov.Visible = True

            _memory.WriteZoomMax(Me.sliderZoomMax.Value / 100.0)
            _memory.WriteFov(Me.sliderFov.Value / 100.0)
        End If
    End Sub





    Private Sub fov_ValueChanged(sender As Object, e As EventArgs) Handles sliderFov.ValueChanged
        If _shouldSaveSettings Then
            My.Settings.FOV = Me.sliderFov.Value / 100.0
            My.Settings.Save()
        End If

        refreshFovLabel()
        _memory.WriteFov(Me.sliderFov.Value / 100.0)
    End Sub

    Private Sub zoomMax_ValueChanged(sender As Object, e As EventArgs) Handles sliderZoomMax.ValueChanged
        If _shouldSaveSettings Then
            My.Settings.ZoomMax = Me.sliderZoomMax.Value / 100.0
            My.Settings.Save()
        End If

        refreshZoomMaxLabel()
        _memory.WriteZoomMax(Me.sliderZoomMax.Value / 100.0)
    End Sub

    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles checkSetZoom.CheckedChanged
        If _shouldSaveSettings Then
            My.Settings.SetZoomCurrent = Me.checkSetZoom.Checked
            My.Settings.Save()
        End If
    End Sub





    Private Sub CheckForUpdateToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CheckForUpdateToolStripMenuItem.Click
        UpdateMemoryAddresses()
    End Sub

    Private Sub UpdateMemoryAddresses()
        Try
            Dim working_date As String = "7/29/2014"

            Dim config_file = New WebClient().DownloadString(MEMORY_ADDRESSES_CONFIG_URL).Split(New Char() {ChrW(10), ChrW(&HD0A), vbCrLf}, StringSplitOptions.RemoveEmptyEntries)

            For Each line In config_file
                Dim setting() = line.Split(New String() {"="}, 2, StringSplitOptions.None)

                Select Case setting(0).Trim
                    Case "WorkingDate"
                        working_date = setting(1)


                    Case "CameraAddress"
                        Dim camera_offsets() = setting(1).Split(New String() {","}, StringSplitOptions.RemoveEmptyEntries)

                        My.Settings.CameraAddress = New Integer(camera_offsets.Length - 1) {}

                        For i As Integer = 0 To camera_offsets.Length - 1
                            My.Settings.CameraAddress(i) = Convert.ToInt32(camera_offsets(i).Trim().Substring(2), 16)
                        Next


                    Case "ZoomCurrentOffset"
                        My.Settings.ZoomCurrentOffset = Convert.ToInt32(setting(1).Trim().Substring(2), 16)

                    Case "ZoomMaxOffset"
                        My.Settings.ZoomMaxOffset = Convert.ToInt32(setting(1).Trim().Substring(2), 16)

                    Case "FovCurrentOffset"
                        My.Settings.FovCurrentOffset = Convert.ToInt32(setting(1).Trim().Substring(2), 16)

                    Case "FovMaxOffset"
                        My.Settings.FovMaxOffset = Convert.ToInt32(setting(1).Trim().Substring(2), 16)
                End Select
            Next

            My.Settings.Save()

            ' recalc addresses using the newly updated ones stored in settings
            _memory.CalculateAddresses()
            
            _memory.WriteFov(Me.sliderFov.Value / 100.0)
            _memory.WriteZoomMax(Me.sliderZoomMax.Value / 100.0)

            MsgBox("Successfully updated to the memory addresses that were working as of: " & vbCrLf & vbCrLf & working_date, MsgBoxStyle.Information, "Updated Memory Addresses")


        Catch ex As WebException
            MsgBox("Timed out attempting to reach the update server:" &
                       vbCrLf & vbCrLf &
                       MEMORY_ADDRESSES_CONFIG_URL &
                       vbCrLf & vbCrLf &
                       "Make sure you have an internet connection and try again.", MsgBoxStyle.Critical, "Connection Timed Out")
        End Try
    End Sub






    Private Sub btnZoomMaxDefault_Click(sender As Object, e As EventArgs) Handles btnZoomMaxDefault.Click
        Me.sliderZoomMax.Value = 20 * 100 ' max zoom default
    End Sub

    Private Sub btnFovDefault_Click(sender As Object, e As EventArgs) Handles btnFovDefault.Click
        Me.sliderFov.Value = 0.78 * 100 ' fov default
    End Sub
End Class
