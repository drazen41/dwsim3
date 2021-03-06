﻿'    Separator Vessel Calculation Routines 
'    Copyright 2008 Daniel Wagner O. de Medeiros
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

Imports Microsoft.MSDN.Samples.GraphicObjects
Imports DWSIM.DWSIM.Flowsheet.FlowSheetSolver

Namespace DWSIM.SimulationObjects.UnitOps

    <System.Serializable()> Public Class Vessel

        Inherits SimulationObjects_UnitOpBaseClass

        Protected m_DQ As Nullable(Of Double)

        Protected m_opmode As OperationMode = OperationMode.TwoPhase

        Protected m_overrideT As Boolean = False
        Protected m_overrideP As Boolean = False
        Protected m_T As Double = 0
        Protected m_P As Double = 0

        Public Enum OperationMode
            TwoPhase = 0
            ThreePhase = 1
        End Enum

        Public Property OpMode() As OperationMode
            Get
                Return m_opmode
            End Get
            Set(ByVal value As OperationMode)
                m_opmode = value
            End Set
        End Property

        Public Property OverrideT() As Boolean
            Get
                Return m_overrideT
            End Get
            Set(ByVal value As Boolean)
                m_overrideT = value
            End Set
        End Property

        Public Property OverrideP() As Boolean
            Get
                Return m_overrideP
            End Get
            Set(ByVal value As Boolean)
                m_overrideP = value
            End Set
        End Property

        Public Property FlashPressure() As Double
            Get
                Return m_P
            End Get
            Set(ByVal value As Double)
                m_P = value
            End Set
        End Property

        Public Property FlashTemperature() As Double
            Get
                Return m_T
            End Get
            Set(ByVal value As Double)
                m_T = value
            End Set
        End Property

        Public Property DeltaQ() As Nullable(Of Double)
            Get
                Return m_DQ
            End Get
            Set(ByVal value As Nullable(Of Double))
                m_DQ = value
            End Set
        End Property

        Public Sub New()
            MyBase.New()
        End Sub

        Public Sub New(ByVal nome As String, ByVal descricao As String)

            MyBase.CreateNew()
            Me.m_ComponentName = nome
            Me.m_ComponentDescription = descricao
            Me.FillNodeItems()
            Me.QTFillNodeItems()
            Me.ShowQuickTable = False

        End Sub

        Public Overrides Function Calculate(Optional ByVal args As Object = Nothing) As Integer

            Dim form As FormFlowsheet = Me.FlowSheet
            Dim objargs As New DWSIM.Outros.StatusChangeEventArgs

            If Not Me.GraphicObject.InputConnectors(0).IsAttached Then
                'Call function to calculate flowsheet
                With objargs
                    .Calculado = False
                    .Nome = Me.Nome
                    .Tipo = TipoObjeto.Vessel
                End With
                CalculateFlowsheet(FlowSheet, objargs, Nothing)
                Throw New Exception(DWSIM.App.GetLocalString("Verifiqueasconexesdo"))
            ElseIf Not Me.GraphicObject.OutputConnectors(0).IsAttached Then
                'Call function to calculate flowsheet
                With objargs
                    .Calculado = False
                    .Nome = Me.Nome
                    .Tipo = TipoObjeto.Vessel
                End With
                CalculateFlowsheet(FlowSheet, objargs, Nothing)
                Throw New Exception(DWSIM.App.GetLocalString("Verifiqueasconexesdo"))
            ElseIf Not Me.GraphicObject.OutputConnectors(1).IsAttached Then
                'Call function to calculate flowsheet
                With objargs
                    .Calculado = False
                    .Nome = Me.Nome
                    .Tipo = TipoObjeto.Vessel
                End With
                CalculateFlowsheet(FlowSheet, objargs, Nothing)
                Throw New Exception(DWSIM.App.GetLocalString("Verifiqueasconexesdo"))
            ElseIf Not Me.GraphicObject.OutputConnectors(2).IsAttached And Me.OpMode = OperationMode.ThreePhase Then
                'Call function to calculate flowsheet
                With objargs
                    .Calculado = False
                    .Nome = Me.Nome
                    .Tipo = TipoObjeto.Vessel
                End With
                CalculateFlowsheet(FlowSheet, objargs, Nothing)
                Throw New Exception(DWSIM.App.GetLocalString("Verifiqueasconexesdo"))
            End If

            If Me.OverrideT = False And Me.OverrideP = False Then

                Dim ems As DWSIM.SimulationObjects.Streams.MaterialStream = form.Collections.CLCS_MaterialStreamCollection(Me.GraphicObject.InputConnectors(0).AttachedConnector.AttachedFrom.Name)
                Dim W As Double = ems.Fases(0).SPMProperties.massflow.GetValueOrDefault
                Dim j As Integer = 0

                Dim ms As DWSIM.SimulationObjects.Streams.MaterialStream
                Dim cp As ConnectionPoint

                cp = Me.GraphicObject.OutputConnectors(0)
                If cp.IsAttached Then
                    ms = form.Collections.CLCS_MaterialStreamCollection(cp.AttachedConnector.AttachedTo.Name)
                    With ms
                        .ClearAllProps()
                        .Fases(0).SPMProperties.temperature = ems.Fases(0).SPMProperties.temperature
                        .Fases(0).SPMProperties.pressure = ems.Fases(0).SPMProperties.pressure
                        .Fases(0).SPMProperties.enthalpy = ems.Fases(2).SPMProperties.enthalpy
                        Dim comp As DWSIM.ClassesBasicasTermodinamica.Substancia
                        j = 0
                        For Each comp In .Fases(0).Componentes.Values
                            comp.FracaoMolar = ems.Fases(2).Componentes(comp.Nome).FracaoMolar
                            comp.FracaoMassica = ems.Fases(2).Componentes(comp.Nome).FracaoMassica
                            j += 1
                        Next
                        For Each comp In .Fases(2).Componentes.Values
                            comp.FracaoMolar = ems.Fases(2).Componentes(comp.Nome).FracaoMolar
                            comp.FracaoMassica = ems.Fases(2).Componentes(comp.Nome).FracaoMassica
                            j += 1
                        Next
                        .Fases(0).SPMProperties.massflow = ems.Fases(2).SPMProperties.massflow
                        .Fases(0).SPMProperties.massfraction = 1
                        .Fases(0).SPMProperties.molarfraction = 1
                        .Fases(3).SPMProperties.massfraction = 0
                        .Fases(3).SPMProperties.molarfraction = 0
                        .Fases(2).SPMProperties.massflow = ems.Fases(2).SPMProperties.massflow
                        .Fases(2).SPMProperties.massfraction = 1
                        .Fases(2).SPMProperties.molarfraction = 1
                    End With
                End If

                cp = Me.GraphicObject.OutputConnectors(1)
                If cp.IsAttached Then
                    ms = form.Collections.CLCS_MaterialStreamCollection(cp.AttachedConnector.AttachedTo.Name)
                    With ms
                        .ClearAllProps()
                        .Fases(0).SPMProperties.temperature = ems.Fases(0).SPMProperties.temperature
                        .Fases(0).SPMProperties.pressure = ems.Fases(0).SPMProperties.pressure
                        .Fases(0).SPMProperties.enthalpy = ems.Fases(3).SPMProperties.enthalpy
                        Dim comp As DWSIM.ClassesBasicasTermodinamica.Substancia
                        j = 0
                        For Each comp In .Fases(0).Componentes.Values
                            comp.FracaoMolar = ems.Fases(3).Componentes(comp.Nome).FracaoMolar
                            comp.FracaoMassica = ems.Fases(3).Componentes(comp.Nome).FracaoMassica
                            j += 1
                        Next
                        For Each comp In .Fases(3).Componentes.Values
                            comp.FracaoMolar = ems.Fases(3).Componentes(comp.Nome).FracaoMolar
                            comp.FracaoMassica = ems.Fases(3).Componentes(comp.Nome).FracaoMassica
                            j += 1
                        Next
                        .Fases(0).SPMProperties.massflow = ems.Fases(3).SPMProperties.massflow
                        .Fases(0).SPMProperties.massfraction = 1
                        .Fases(0).SPMProperties.molarfraction = 1
                        .Fases(3).SPMProperties.massflow = ems.Fases(3).SPMProperties.massflow
                        .Fases(3).SPMProperties.massfraction = 1
                        .Fases(3).SPMProperties.molarfraction = 1
                        .Fases(2).SPMProperties.massfraction = 0
                        .Fases(2).SPMProperties.molarfraction = 0
                    End With
                End If

                cp = Me.GraphicObject.OutputConnectors(2)
                If cp.IsAttached Then
                    ms = form.Collections.CLCS_MaterialStreamCollection(cp.AttachedConnector.AttachedTo.Name)
                    With ms
                        .ClearAllProps()
                        If ems.Fases(6).SPMProperties.molarflow.GetValueOrDefault > 0 Then
                            .Fases(0).SPMProperties.temperature = ems.Fases(0).SPMProperties.temperature
                            .Fases(0).SPMProperties.pressure = ems.Fases(0).SPMProperties.pressure
                            .Fases(0).SPMProperties.enthalpy = ems.Fases(6).SPMProperties.enthalpy
                            Dim comp As DWSIM.ClassesBasicasTermodinamica.Substancia
                            j = 0
                            For Each comp In .Fases(0).Componentes.Values
                                comp.FracaoMolar = ems.Fases(6).Componentes(comp.Nome).FracaoMolar
                                comp.FracaoMassica = ems.Fases(6).Componentes(comp.Nome).FracaoMassica
                                j += 1
                            Next
                            For Each comp In .Fases(6).Componentes.Values
                                comp.FracaoMolar = ems.Fases(6).Componentes(comp.Nome).FracaoMolar
                                comp.FracaoMassica = ems.Fases(6).Componentes(comp.Nome).FracaoMassica
                                j += 1
                            Next
                            .Fases(0).SPMProperties.massflow = ems.Fases(6).SPMProperties.massflow
                            .Fases(0).SPMProperties.massfraction = 1
                            .Fases(0).SPMProperties.molarfraction = 1
                            .Fases(6).SPMProperties.massflow = ems.Fases(6).SPMProperties.massflow
                            .Fases(6).SPMProperties.massfraction = 1
                            .Fases(6).SPMProperties.molarfraction = 1
                            .Fases(2).SPMProperties.massfraction = 0
                            .Fases(2).SPMProperties.molarfraction = 0
                        Else
                            .Fases(0).SPMProperties.temperature = ems.Fases(0).SPMProperties.temperature
                            .Fases(0).SPMProperties.pressure = ems.Fases(0).SPMProperties.pressure
                            .Fases(0).SPMProperties.enthalpy = ems.Fases(4).SPMProperties.enthalpy
                            Dim comp As DWSIM.ClassesBasicasTermodinamica.Substancia
                            j = 0
                            For Each comp In .Fases(0).Componentes.Values
                                comp.FracaoMolar = ems.Fases(4).Componentes(comp.Nome).FracaoMolar
                                comp.FracaoMassica = ems.Fases(4).Componentes(comp.Nome).FracaoMassica
                                j += 1
                            Next
                            For Each comp In .Fases(4).Componentes.Values
                                comp.FracaoMolar = ems.Fases(4).Componentes(comp.Nome).FracaoMolar
                                comp.FracaoMassica = ems.Fases(4).Componentes(comp.Nome).FracaoMassica
                                j += 1
                            Next
                            .Fases(0).SPMProperties.massflow = ems.Fases(4).SPMProperties.massflow
                            .Fases(0).SPMProperties.massfraction = 1
                            .Fases(0).SPMProperties.molarfraction = 1
                            .Fases(4).SPMProperties.massflow = ems.Fases(4).SPMProperties.massflow
                            .Fases(4).SPMProperties.massfraction = 1
                            .Fases(4).SPMProperties.molarfraction = 1
                            .Fases(2).SPMProperties.massfraction = 0
                            .Fases(2).SPMProperties.molarfraction = 0
                        End If
                    End With
                End If

            Else

                Dim xl, xv, T, P, H, Hv, Hl, Tv, Tl, S, wtotalx, wtotaly As Double
                Dim ems As DWSIM.SimulationObjects.Streams.MaterialStream = form.Collections.CLCS_MaterialStreamCollection(Me.GraphicObject.InputConnectors(0).AttachedConnector.AttachedFrom.Name)
                Dim W As Double = ems.Fases(0).SPMProperties.massflow.GetValueOrDefault
                Dim tmp As Object

                If Me.OverrideP Then
                    P = Me.FlashPressure
                Else
                    P = ems.Fases(0).SPMProperties.pressure.GetValueOrDefault
                End If
                If Me.OverrideT Then
                    T = Me.FlashTemperature
                    Tl = T
                    Tv = T
                Else
                    T = ems.Fases(0).SPMProperties.temperature.GetValueOrDefault
                    Tl = T
                    Tv = T
                End If

                Me.PropertyPackage.CurrentMaterialStream = ems

                H = ems.Fases(0).SPMProperties.enthalpy.GetValueOrDefault

                If FlowSheet.Options.SempreCalcularFlashPH Then
                    tmp = Me.PropertyPackage.DW_CalcEquilibrio_ISOL(PropertyPackages.FlashSpec.P, PropertyPackages.FlashSpec.H, P, H, T)
                    If ems.Fases(0).Componentes.Count = 1 Then
                        Tv = Me.PropertyPackage.DW_CalcDewT(New Double() {1}, P)(0)
                        Tl = Me.PropertyPackage.DW_CalcBubT(New Double() {1}, P)(0)
                    Else
                        Tv = T
                        Tl = T
                    End If
                Else
                    tmp = Me.PropertyPackage.DW_CalcEquilibrio_ISOL(PropertyPackages.FlashSpec.T, PropertyPackages.FlashSpec.P, T, P, 0)
                End If

                'Return New Object() {xl, xv, T, P, H, S, 1, 1, Vx, Vy}
                Dim Vx(ems.Fases(0).Componentes.Count - 1), Vy(ems.Fases(0).Componentes.Count - 1), Vwx(ems.Fases(0).Componentes.Count - 1), Vwy(ems.Fases(0).Componentes.Count - 1) As Double
                xl = tmp(0)
                xv = tmp(1)
                T = tmp(2)
                P = tmp(3)
                H = tmp(4)
                S = tmp(5)
                Vx = tmp(8)
                Vy = tmp(9)

                Hv = Me.PropertyPackage.DW_CalcEnthalpy(Vy, T, P, PropertyPackages.State.Vapor)
                Hl = Me.PropertyPackage.DW_CalcEnthalpy(Vx, T, P, PropertyPackages.State.Liquid)

                Dim i As Integer = 0
                Dim j As Integer = 0

                Dim ms As DWSIM.SimulationObjects.Streams.MaterialStream
                Dim cp As ConnectionPoint
                cp = Me.GraphicObject.InputConnectors(0)
                If cp.IsAttached Then
                    ms = form.Collections.CLCS_MaterialStreamCollection(cp.AttachedConnector.AttachedFrom.Name)
                    Dim comp As DWSIM.ClassesBasicasTermodinamica.Substancia
                    wtotalx = 0.0#
                    wtotaly = 0.0#
                    i = 0
                    For Each comp In ms.Fases(0).Componentes.Values
                        wtotalx += Vx(i) * comp.ConstantProperties.Molar_Weight
                        wtotaly += Vy(i) * comp.ConstantProperties.Molar_Weight
                        i += 1
                    Next
                    i = 0
                    For Each comp In ms.Fases(0).Componentes.Values
                        Vwx(i) = Vx(i) * comp.ConstantProperties.Molar_Weight / wtotalx
                        Vwy(i) = Vy(i) * comp.ConstantProperties.Molar_Weight / wtotaly
                        i += 1
                    Next
                End If

                cp = Me.GraphicObject.OutputConnectors(0)
                If cp.IsAttached Then
                    ms = form.Collections.CLCS_MaterialStreamCollection(cp.AttachedConnector.AttachedTo.Name)
                    With ms
                        .ClearAllProps()
                        .Fases(0).SPMProperties.temperature = Tv
                        .Fases(0).SPMProperties.pressure = P
                        .Fases(0).SPMProperties.enthalpy = Hv
                        Dim comp As DWSIM.ClassesBasicasTermodinamica.Substancia
                        j = 0
                        For Each comp In .Fases(0).Componentes.Values
                            comp.FracaoMolar = Vy(j)
                            comp.FracaoMassica = Vwy(j)
                            j += 1
                        Next
                        j = 0
                        For Each comp In .Fases(2).Componentes.Values
                            comp.FracaoMolar = Vy(j)
                            comp.FracaoMassica = Vwy(j)
                            j += 1
                        Next
                        .Fases(0).SPMProperties.massflow = W * (wtotaly * xv / (wtotaly * xv + wtotalx * xl))
                        .Fases(2).SPMProperties.massflow = W * (wtotaly * xv / (wtotaly * xv + wtotalx * xl))
                        .Fases(3).SPMProperties.massfraction = 0
                        .Fases(3).SPMProperties.molarfraction = 0
                        .Fases(2).SPMProperties.massfraction = 1
                        .Fases(2).SPMProperties.molarfraction = 1
                    End With
                End If

                cp = Me.GraphicObject.OutputConnectors(1)
                If cp.IsAttached Then
                    ms = form.Collections.CLCS_MaterialStreamCollection(cp.AttachedConnector.AttachedTo.Name)
                    With ms
                        .ClearAllProps()
                        .Fases(0).SPMProperties.temperature = Tl
                        .Fases(0).SPMProperties.pressure = P
                        .Fases(0).SPMProperties.enthalpy = Hl
                        Dim comp As DWSIM.ClassesBasicasTermodinamica.Substancia
                        j = 0
                        For Each comp In .Fases(0).Componentes.Values
                            comp.FracaoMolar = Vx(j)
                            comp.FracaoMassica = Vwx(j)
                            j += 1
                        Next
                        j = 0
                        For Each comp In .Fases(3).Componentes.Values
                            comp.FracaoMolar = Vx(j)
                            comp.FracaoMassica = Vwx(j)
                            j += 1
                        Next
                        .Fases(0).SPMProperties.massflow = W * (wtotalx * xl / (wtotaly * xv + wtotalx * xl))
                        .Fases(3).SPMProperties.massflow = W * (wtotalx * xl / (wtotaly * xv + wtotalx * xl))
                        .Fases(3).SPMProperties.massfraction = 1
                        .Fases(3).SPMProperties.molarfraction = 1
                        .Fases(2).SPMProperties.massfraction = 0
                        .Fases(2).SPMProperties.molarfraction = 0
                    End With
                End If

                cp = Me.GraphicObject.OutputConnectors(2)
                If cp.IsAttached Then
                    ms = form.Collections.CLCS_MaterialStreamCollection(cp.AttachedConnector.AttachedTo.Name)
                    With ms
                        .ClearAllProps()
                    End With
                End If

            End If


            'Call function to calculate flowsheet
            With objargs
                .Calculado = True
                .Nome = Me.Nome
                .Tag = Me.GraphicObject.Tag
                .Tipo = TipoObjeto.Vessel
            End With

            form.CalculationQueue.Enqueue(objargs)

        End Function

        Public Overrides Function DeCalculate() As Integer

            'If Not Me.GraphicObject.InputConnectors(0).IsAttached Then Throw New Exception(DWSIM.App.GetLocalString("Nohcorrentedematriac10"))
            'If Not Me.GraphicObject.OutputConnectors(0).IsAttached Then Throw New Exception(DWSIM.App.GetLocalString("Nohcorrentedematriac11"))
            'If Not Me.GraphicObject.OutputConnectors(1).IsAttached Then Throw New Exception(DWSIM.App.GetLocalString("Nohcorrentedematriac11"))

            Dim form As Global.DWSIM.FormFlowsheet = Me.Flowsheet

            Dim j As Integer = 0

            Dim ms As DWSIM.SimulationObjects.Streams.MaterialStream
            Dim cp As ConnectionPoint

            cp = Me.GraphicObject.OutputConnectors(0)
            If cp.IsAttached Then
                ms = form.Collections.CLCS_MaterialStreamCollection(cp.AttachedConnector.AttachedTo.Name)
                With ms
                    .Fases(0).SPMProperties.temperature = Nothing
                    .Fases(0).SPMProperties.pressure = Nothing
                    .Fases(0).SPMProperties.enthalpy = Nothing
                    Dim comp As DWSIM.ClassesBasicasTermodinamica.Substancia
                    j = 0
                    For Each comp In .Fases(0).Componentes.Values
                        comp.FracaoMolar = 0
                        comp.FracaoMassica = 0
                        j += 1
                    Next
                    .Fases(0).SPMProperties.massflow = Nothing
                    .Fases(0).SPMProperties.massfraction = 1
                    .Fases(0).SPMProperties.molarfraction = 1
                    .GraphicObject.Calculated = False
                End With
            End If

            cp = Me.GraphicObject.OutputConnectors(1)
            If cp.IsAttached Then
                ms = form.Collections.CLCS_MaterialStreamCollection(cp.AttachedConnector.AttachedTo.Name)
                With ms
                    .Fases(0).SPMProperties.temperature = Nothing
                    .Fases(0).SPMProperties.pressure = Nothing
                    .Fases(0).SPMProperties.enthalpy = Nothing
                    Dim comp As DWSIM.ClassesBasicasTermodinamica.Substancia
                    j = 0
                    For Each comp In .Fases(0).Componentes.Values
                        comp.FracaoMolar = 0
                        comp.FracaoMassica = 0
                        j += 1
                    Next
                    .Fases(0).SPMProperties.massflow = Nothing
                    .Fases(0).SPMProperties.massfraction = 1
                    .Fases(0).SPMProperties.molarfraction = 1
                    .GraphicObject.Calculated = False
                End With
            End If

            'Call function to calculate flowsheet
            Dim objargs As New DWSIM.Outros.StatusChangeEventArgs
            With objargs
                .Calculado = False
                .Nome = Me.Nome
                .Tipo = TipoObjeto.Vessel
            End With

            form.CalculationQueue.Enqueue(objargs)

        End Function

        Public Overloads Overrides Sub UpdatePropertyNodes(ByVal su As SistemasDeUnidades.Unidades, ByVal nf As String)

        End Sub

        Public Overrides Sub QTFillNodeItems()

        End Sub

        Public Overrides Sub PopulatePropertyGrid(ByRef pgrid As PropertyGridEx.PropertyGridEx, ByVal su As SistemasDeUnidades.Unidades)

            Dim Conversor As New DWSIM.SistemasDeUnidades.Conversor

            With pgrid

                .PropertySort = PropertySort.Categorized
                .ShowCustomProperties = True
                .Item.Clear()

                MyBase.PopulatePropertyGrid(pgrid, su)

                Dim ent, saida1, saida2, saida3 As String
                If Me.GraphicObject.InputConnectors(0).IsAttached = True Then
                    ent = Me.GraphicObject.InputConnectors(0).AttachedConnector.AttachedFrom.Tag
                Else
                    ent = ""
                End If
                If Me.GraphicObject.OutputConnectors(0).IsAttached = True Then
                    saida1 = Me.GraphicObject.OutputConnectors(0).AttachedConnector.AttachedTo.Tag
                Else
                    saida1 = ""
                End If
                If Me.GraphicObject.OutputConnectors(1).IsAttached = True Then
                    saida2 = Me.GraphicObject.OutputConnectors(1).AttachedConnector.AttachedTo.Tag
                Else
                    saida2 = ""
                End If
                If Me.GraphicObject.OutputConnectors(2).IsAttached = True Then
                    saida3 = Me.GraphicObject.OutputConnectors(2).AttachedConnector.AttachedTo.Tag
                Else
                    saida3 = ""
                End If

                .Item.Add(DWSIM.App.GetLocalString("Correntedeentrada"), ent, False, DWSIM.App.GetLocalString("Conexes1"), "", True)
                With .Item(.Item.Count - 1)
                    .DefaultValue = Nothing
                    .CustomEditor = New DWSIM.Editors.Streams.UIInputMSSelector
                End With

                .Item.Add(DWSIM.App.GetLocalString("Saidadevapor"), saida1, False, DWSIM.App.GetLocalString("Conexes1"), "", True)
                With .Item(.Item.Count - 1)
                    .DefaultValue = Nothing
                    .CustomEditor = New DWSIM.Editors.Streams.UIOutputMSSelector
                End With

                .Item.Add(DWSIM.App.GetLocalString("Saidadelquido"), saida2, False, DWSIM.App.GetLocalString("Conexes1"), "", True)
                With .Item(.Item.Count - 1)
                    .DefaultValue = Nothing
                    .CustomEditor = New DWSIM.Editors.Streams.UIOutputMSSelector
                End With

                .Item.Add(DWSIM.App.GetLocalString("Saidadelquido") & " (2)", saida3, False, DWSIM.App.GetLocalString("Conexes1"), "", True)
                With .Item(.Item.Count - 1)
                    .DefaultValue = Nothing
                    .CustomEditor = New DWSIM.Editors.Streams.UIOutputMSSelector
                End With

                .Item.Add(DWSIM.App.GetLocalString("VesselOperatingMode"), Me, "OpMode", False, DWSIM.App.GetLocalString("Parmetros2"), DWSIM.App.GetLocalString("VesselOperatingModeDesc"), True)

                .Item.Add(DWSIM.App.GetLocalString("SobreporTemperaturad"), Me, "OverrideT", False, DWSIM.App.GetLocalString("Parmetros2"), DWSIM.App.GetLocalString("SelecioLiquidrueparaign4"), True)
                If Me.OverrideT Then
                    Dim valor = Format(Conversor.ConverterDoSI(su.spmp_temperature, Me.FlashTemperature), FlowSheet.Options.NumberFormat)
                    .Item.Add(FT(DWSIM.App.GetLocalString("Temperatura"), su.spmp_temperature), valor, False, DWSIM.App.GetLocalString("Parmetros2"), DWSIM.App.GetLocalString("Temperaturadeseparao"), True)
                    With .Item(.Item.Count - 1)
                        .Tag = New Object() {FlowSheet.Options.NumberFormat, su.spmp_temperature, "T"}
                        .CustomEditor = New DWSIM.Editors.Generic.UIUnitConverter
                    End With
                End If
                .Item.Add(DWSIM.App.GetLocalString("SobreporPressodesepa"), Me, "OverrideP", False, DWSIM.App.GetLocalString("Parmetros2"), DWSIM.App.GetLocalString("SelecioLiquidrueparaign5"), True)
                If Me.OverrideP Then
                    Dim valor = Format(Conversor.ConverterDoSI(su.spmp_pressure, Me.FlashPressure), FlowSheet.Options.NumberFormat)
                    .Item.Add(FT(DWSIM.App.GetLocalString("Presso"), su.spmp_pressure), valor, False, DWSIM.App.GetLocalString("Parmetros2"), DWSIM.App.GetLocalString("Pressodeseparao"), True)
                    With .Item(.Item.Count - 1)
                        .Tag = New Object() {FlowSheet.Options.NumberFormat, su.spmp_pressure, "P"}
                        .CustomEditor = New DWSIM.Editors.Generic.UIUnitConverter
                    End With
                End If

                If Me.IsSpecAttached = True Then
                    .Item.Add(DWSIM.App.GetLocalString("ObjetoUtilizadopor"), FlowSheet.Collections.ObjectCollection(Me.AttachedSpecId).GraphicObject.Tag, True, DWSIM.App.GetLocalString("Miscelnea2"), "", True)
                    .Item.Add(DWSIM.App.GetLocalString("Utilizadocomo"), Me.SpecVarType, True, DWSIM.App.GetLocalString("Miscelnea3"), "", True)
                End If

                If Not Me.Annotation Is Nothing Then
                    .Item.Add(DWSIM.App.GetLocalString("Anotaes"), Me, "Annotation", False, DWSIM.App.GetLocalString("Outros"), DWSIM.App.GetLocalString("Cliquenobotocomretic"), True)
                    With .Item(.Item.Count - 1)
                        .IsBrowsable = False
                        .CustomEditor = New DWSIM.Editors.Annotation.UIAnnotationEditor
                    End With
                End If

                .ExpandAllGridItems()

            End With

        End Sub

        Public Overrides Function GetPropertyValue(ByVal prop As String, Optional ByVal su As SistemasDeUnidades.Unidades = Nothing) As Object
            If su Is Nothing Then su = New DWSIM.SistemasDeUnidades.UnidadesSI
            Dim cv As New DWSIM.SistemasDeUnidades.Conversor
            Dim value As Double = 0
            Dim propidx As Integer = CInt(prop.Split("_")(2))

            Select Case propidx

                Case 0
                    'PROP_SV_0	Separation Temperature
                    value = cv.ConverterDoSI(su.spmp_temperature, Me.FlashTemperature)
                Case 1
                    'PROP_SV_1	Separation Pressure
                    value = cv.ConverterDoSI(su.spmp_pressure, Me.FlashPressure)

            End Select

            Return value
        End Function

        Public Overloads Overrides Function GetProperties(ByVal proptype As SimulationObjects_BaseClass.PropertyType) As String()
            Dim i As Integer = 0
            Dim proplist As New ArrayList
            Select Case proptype
                Case PropertyType.RW
                    For i = 0 To 1
                        proplist.Add("PROP_SV_" + CStr(i))
                    Next
                Case PropertyType.WR
                    For i = 0 To 1
                        proplist.Add("PROP_SV_" + CStr(i))
                    Next
                Case PropertyType.ALL
                    For i = 0 To 1
                        proplist.Add("PROP_SV_" + CStr(i))
                    Next
            End Select
            Return proplist.ToArray(GetType(System.String))
            proplist = Nothing
        End Function

        Public Overrides Function SetPropertyValue(ByVal prop As String, ByVal propval As Object, Optional ByVal su As DWSIM.SistemasDeUnidades.Unidades = Nothing) As Object
            If su Is Nothing Then su = New DWSIM.SistemasDeUnidades.UnidadesSI
            Dim cv As New DWSIM.SistemasDeUnidades.Conversor
            Dim propidx As Integer = CInt(prop.Split("_")(2))

            Select Case propidx
                Case 0
                    'PROP_SV_0	Separation Temperature
                    Me.FlashTemperature = cv.ConverterParaSI(su.spmp_temperature, propval)
                Case 1
                    'PROP_SV_1	Separation Pressure
                    Me.FlashPressure = cv.ConverterParaSI(su.spmp_pressure, propval)
            End Select
            Return 1
        End Function

        Public Overrides Function GetPropertyUnit(ByVal prop As String, Optional ByVal su As SistemasDeUnidades.Unidades = Nothing) As Object
            If su Is Nothing Then su = New DWSIM.SistemasDeUnidades.UnidadesSI
            Dim cv As New DWSIM.SistemasDeUnidades.Conversor
            Dim value As String = ""
            Dim propidx As Integer = CInt(prop.Split("_")(2))

            Select Case propidx

                Case 0
                    'PROP_SV_0	Separation Temperature
                    value = su.spmp_temperature
                Case 1
                    'PROP_SV_1	Separation Pressure
                    value = su.spmp_pressure

            End Select

            Return value
        End Function
    End Class

End Namespace
