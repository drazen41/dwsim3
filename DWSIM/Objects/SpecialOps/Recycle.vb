﻿'    Recycle Calculation Routines 
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
Imports DWSIM.DWSIM.SimulationObjects.SpecialOps.Helpers.Recycle
Imports System.Linq
Imports System.ComponentModel
Imports PropertyGridEx

Namespace DWSIM.SimulationObjects.SpecialOps

    <System.Serializable()> Public Class Recycle

        Inherits SimulationObjects_UnitOpBaseClass

        Protected m_ConvPar As ConvergenceParameters
        Protected m_ConvHist As ConvergenceHistory
        Protected m_AccelMethod As AccelMethod = AccelMethod.Wegstein
        Protected m_WegPars As WegsteinParameters
        Protected m_FlashType As FlashType

        Protected m_MaxIterations As Integer = 10
        Protected m_IterationCount As Integer = 0
        Protected m_InternalCounterT As Integer = 0
        Protected m_InternalCounterP As Integer = 0
        Protected m_InternalCounterW As Integer = 0
        Protected m_IterationsTaken As Integer = 0

        Public Overrides Function LoadData(data As System.Collections.Generic.List(Of System.Xml.Linq.XElement)) As Boolean

            MyBase.LoadData(data)

            Dim ci As Globalization.CultureInfo = Globalization.CultureInfo.InvariantCulture

            MyBase.LoadData(data)
            Dim xel As XElement

            xel = (From xel2 As XElement In data Select xel2 Where xel2.Name = "WegPars").SingleOrDefault

            If Not xel Is Nothing Then
                m_WegPars.AccelDelay = Double.Parse(xel.@AccelDelay, ci)
                m_WegPars.AccelFreq = Double.Parse(xel.@AccelFreq, ci)
                m_WegPars.Qmax = Double.Parse(xel.@Qmax, ci)
                m_WegPars.Qmin = Double.Parse(xel.@Qmin, ci)
            End If

        End Function

        Public Overrides Function SaveData() As System.Collections.Generic.List(Of System.Xml.Linq.XElement)

            Dim elements As System.Collections.Generic.List(Of System.Xml.Linq.XElement) = MyBase.SaveData()
            Dim ci As Globalization.CultureInfo = Globalization.CultureInfo.InvariantCulture

            With elements
                .Add(New XElement("WegPars", New XAttribute("AccelDelay", m_WegPars.AccelDelay),
                                  New XAttribute("AccelFreq", m_WegPars.AccelFreq),
                                  New XAttribute("Qmax", m_WegPars.Qmax),
                                  New XAttribute("Qmin", m_WegPars.Qmin)))
            End With

            Return elements

        End Function

        Public Property IterationsTaken() As Integer
            Get
                Return m_IterationsTaken
            End Get
            Set(ByVal value As Integer)
                m_IterationsTaken = value
            End Set
        End Property

        Public Property IterationCount() As Integer
            Get
                Return m_IterationCount
            End Get
            Set(ByVal value As Integer)
                m_IterationCount = value
            End Set
        End Property

        Public Property FlashType() As FlashType
            Get
                Return m_FlashType
            End Get
            Set(ByVal value As FlashType)
                m_FlashType = value
            End Set
        End Property

        Public Property WegsteinParameters() As WegsteinParameters
            Get
                Return m_WegPars
            End Get
            Set(ByVal value As WegsteinParameters)
                m_WegPars = value
            End Set
        End Property

        Public Property AccelerationMethod() As AccelMethod
            Get
                Return m_AccelMethod
            End Get
            Set(ByVal value As AccelMethod)
                m_AccelMethod = value
            End Set
        End Property

        Public Property ConvergenceParameters() As ConvergenceParameters
            Get
                Return m_ConvPar
            End Get
            Set(ByVal value As ConvergenceParameters)
                m_ConvPar = value
            End Set
        End Property

        Public Property ConvergenceHistory() As ConvergenceHistory
            Get
                Return m_ConvHist
            End Get
            Set(ByVal value As ConvergenceHistory)
                m_ConvHist = value
            End Set
        End Property

        Public Property MaximumIterations() As Integer
            Get
                Return Me.m_MaxIterations
            End Get
            Set(ByVal value As Integer)
                Me.m_MaxIterations = value
            End Set
        End Property

        Public Sub New()

            MyBase.CreateNew()

            m_ConvPar = New ConvergenceParameters
            m_ConvHist = New ConvergenceHistory
            m_WegPars = New WegsteinParameters

        End Sub

        Public Sub New(ByVal nome As String, ByVal descricao As String)

            MyBase.CreateNew()

            m_ConvPar = New ConvergenceParameters
            m_ConvHist = New ConvergenceHistory
            m_WegPars = New WegsteinParameters

            Me.m_ComponentName = nome
            Me.m_ComponentDescription = descricao
            Me.FillNodeItems()
            Me.QTFillNodeItems()

        End Sub

        Public Overrides Sub QTFillNodeItems()

            With Me.QTNodeTableItems

                .Clear()

                .Add(0, New DWSIM.Outros.NodeItem(DWSIM.App.GetLocalString("Iteraes"), "", "", 0, 0, ""))
                .Add(1, New DWSIM.Outros.NodeItem(DWSIM.App.GetLocalString("ErroT"), "", "", 1, 0, ""))
                .Add(2, New DWSIM.Outros.NodeItem(DWSIM.App.GetLocalString("ErroP"), "", "", 2, 0, ""))
                .Add(3, New DWSIM.Outros.NodeItem(DWSIM.App.GetLocalString("ErroW"), "", "", 3, 0, ""))

            End With

        End Sub

        Public Overrides Sub UpdatePropertyNodes(ByVal su As SistemasDeUnidades.Unidades, ByVal nf As String)

            Dim Conversor As New DWSIM.SistemasDeUnidades.Conversor
            If Me.NodeTableItems Is Nothing Then
                Me.NodeTableItems = New System.Collections.Generic.Dictionary(Of Integer, DWSIM.Outros.NodeItem)
                Me.FillNodeItems()
            End If

            Try

                For Each nti As Outros.NodeItem In Me.NodeTableItems.Values
                    nti.Value = GetPropertyValue(nti.Text, FlowSheet.Options.SelectedUnitSystem)
                    nti.Unit = GetPropertyUnit(nti.Text, FlowSheet.Options.SelectedUnitSystem)
                Next

                If Me.QTNodeTableItems Is Nothing Then
                    Me.QTNodeTableItems = New System.Collections.Generic.Dictionary(Of Integer, DWSIM.Outros.NodeItem)
                    Me.QTFillNodeItems()
                End If

                With Me.QTNodeTableItems

                    Dim valor As String

                    .Item(0).Value = Me.IterationsTaken
                    .Item(0).Unit = ""

                    valor = Format(Conversor.ConverterDoSI(su.spmp_deltaT, Me.ConvergenceHistory.TemperaturaE), nf)
                    .Item(1).Value = valor
                    .Item(1).Unit = su.spmp_deltaT

                    valor = Format(Conversor.ConverterDoSI(su.spmp_deltaP, Me.ConvergenceHistory.PressaoE), nf)
                    .Item(2).Value = valor
                    .Item(2).Unit = su.spmp_deltaP

                    valor = Format(Conversor.ConverterDoSI(su.spmp_massflow, Me.ConvergenceHistory.VazaoMassicaE), nf)
                    .Item(3).Value = valor
                    .Item(3).Unit = su.spmp_massflow

                End With

            Catch ex As Exception

            End Try


        End Sub

        Public Overrides Function Calculate(Optional ByVal args As Object = Nothing) As Integer

            Dim form As Global.DWSIM.FormFlowsheet = Me.Flowsheet
            Dim objargs As New DWSIM.Outros.StatusChangeEventArgs

            If Not Me.GraphicObject.OutputConnectors(0).IsAttached Then
                'Call function to calculate flowsheet
                With objargs
                    .Calculado = False
                    .Nome = Me.Nome
                    .Tipo = TipoObjeto.OT_Reciclo
                End With
                'CalculateFlowsheet(Flowsheet, objargs, Nothing)
                Throw New Exception(DWSIM.App.GetLocalString("Nohcorrentedematriac7"))
            ElseIf Not Me.GraphicObject.InputConnectors(0).IsAttached Then
                'Call function to calculate flowsheet
                With objargs
                    .Calculado = False
                    .Nome = Me.Nome
                    .Tipo = TipoObjeto.OT_Reciclo
                End With
                'CalculateFlowsheet(Flowsheet, objargs, Nothing)
                Throw New Exception(DWSIM.App.GetLocalString("Verifiqueasconexesdo"))
            End If

            Dim Tnew, Pnew, Wnew As Double

            Dim ems As DWSIM.SimulationObjects.Streams.MaterialStream = form.Collections.CLCS_MaterialStreamCollection(Me.GraphicObject.InputConnectors(0).AttachedConnector.AttachedFrom.Name)
            With ems.Fases(0).SPMProperties

                Me.ConvergenceHistory.TemperaturaE = .temperature.GetValueOrDefault - Me.ConvergenceHistory.Temperatura
                Me.ConvergenceHistory.PressaoE = .pressure.GetValueOrDefault - Me.ConvergenceHistory.Pressao
                Me.ConvergenceHistory.VazaoMassicaE = .massflow.GetValueOrDefault - Me.ConvergenceHistory.VazaoMassica

                Me.ConvergenceHistory.TemperaturaE0 = Me.ConvergenceHistory.Temperatura - Me.ConvergenceHistory.Temperatura0
                Me.ConvergenceHistory.PressaoE0 = Me.ConvergenceHistory.Pressao - Me.ConvergenceHistory.Pressao0
                Me.ConvergenceHistory.VazaoMassicaE0 = Me.ConvergenceHistory.VazaoMassica - Me.ConvergenceHistory.VazaoMassica0

                Me.ConvergenceHistory.Temperatura0 = Me.ConvergenceHistory.Temperatura
                Me.ConvergenceHistory.Pressao0 = Me.ConvergenceHistory.Pressao
                Me.ConvergenceHistory.VazaoMassica0 = Me.ConvergenceHistory.VazaoMassica

                Me.ConvergenceHistory.Temperatura = .temperature.GetValueOrDefault
                Me.ConvergenceHistory.Pressao = .pressure.GetValueOrDefault
                Me.ConvergenceHistory.VazaoMassica = .massflow.GetValueOrDefault

            End With

            If Me.IterationCount <= 3 Then

SS:             Tnew = Me.ConvergenceHistory.Temperatura
                Pnew = Me.ConvergenceHistory.Pressao
                Wnew = Me.ConvergenceHistory.VazaoMassica

            Else

                Select Case Me.AccelerationMethod

                    Case AccelMethod.None

                        GoTo SS

                    Case AccelMethod.Wegstein

                        If Me.WegsteinParameters.AccelDelay <= Me.IterationCount + 3 Then

                            Dim sT, sP, sW As Double
                            Dim qT, qP, qW As Double
                            sT = (Me.ConvergenceHistory.TemperaturaE - Me.ConvergenceHistory.TemperaturaE0) / (Me.ConvergenceHistory.Temperatura - Me.ConvergenceHistory.Temperatura0)
                            sP = (Me.ConvergenceHistory.PressaoE - Me.ConvergenceHistory.PressaoE0) / (Me.ConvergenceHistory.Pressao - Me.ConvergenceHistory.Pressao0)
                            sW = (Me.ConvergenceHistory.VazaoMassicaE - Me.ConvergenceHistory.VazaoMassicaE0) / (Me.ConvergenceHistory.VazaoMassica - Me.ConvergenceHistory.VazaoMassica0)
                            qT = sT / (sT - 1)
                            qP = sP / (sP - 1)
                            qW = sW / (sW - 1)
                            If Me.WegsteinParameters.AccelFreq <= Me.m_InternalCounterT And Double.IsNaN(sT) = False And qT > Me.WegsteinParameters.Qmin And qT < Me.WegsteinParameters.Qmax Then
                                Tnew = Me.ConvergenceHistory.TemperaturaE * (1 - qT) + Me.ConvergenceHistory.Temperatura * qT
                                Me.m_InternalCounterT = 0
                            Else
                                Tnew = Me.ConvergenceHistory.Temperatura
                                Me.m_InternalCounterT += 1
                            End If
                            If Me.WegsteinParameters.AccelFreq <= Me.m_InternalCounterP And Double.IsNaN(sP) = False And qP > Me.WegsteinParameters.Qmin And qP < Me.WegsteinParameters.Qmax Then
                                Pnew = Me.ConvergenceHistory.PressaoE * (1 - qP) + Me.ConvergenceHistory.Pressao * qP
                                Me.m_InternalCounterP = 0
                            Else
                                Pnew = Me.ConvergenceHistory.Pressao
                                Me.m_InternalCounterP += 1
                            End If
                            If Me.WegsteinParameters.AccelFreq <= Me.m_InternalCounterW And Double.IsNaN(sW) = False And qW > Me.WegsteinParameters.Qmin And qW < Me.WegsteinParameters.Qmax Then
                                Wnew = Me.ConvergenceHistory.VazaoMassicaE * (1 - qW) + Me.ConvergenceHistory.VazaoMassica * qW
                                Me.m_InternalCounterW = 0
                            Else
                                Wnew = Me.ConvergenceHistory.VazaoMassica
                                Me.m_InternalCounterW += 1
                            End If

                        Else

                            GoTo SS

                        End If

                    Case AccelMethod.Dominant_Eigenvalue

                        Dim eT(1), eP(1), eW(1), M As Double

                        eT(0) = Me.ConvergenceHistory.TemperaturaE0
                        eT(1) = Me.ConvergenceHistory.TemperaturaE
                        eP(0) = Me.ConvergenceHistory.PressaoE0
                        eP(1) = Me.ConvergenceHistory.PressaoE
                        eW(0) = Me.ConvergenceHistory.VazaoMassicaE0
                        eW(1) = Me.ConvergenceHistory.VazaoMassicaE

                        Dim ve0 As Double() = New Double() {Math.Abs(eT(0)), Math.Abs(eP(0)), Math.Abs(eW(0))}
                        Dim ve1 As Double() = New Double() {Math.Abs(eT(1)), Math.Abs(eP(1)), Math.Abs(eW(1))}

                        M = MAX(ve1) / MAX(ve0)

                        With Me.ConvergenceHistory
                            If Double.IsNaN(M) = False Then
                                Tnew = .Temperatura0 + (.Temperatura - .Temperatura0) / (1 - M)
                                Pnew = .Pressao0 + (.Pressao - .Pressao0) / (1 - M)
                                Wnew = .VazaoMassica0 + (.VazaoMassica - .VazaoMassica0) / (1 - M)
                            Else
                                Tnew = .Temperatura
                                Pnew = .Pressao
                                Wnew = .VazaoMassica
                            End If
                        End With

                End Select

            End If

            Dim tmp As Object
            Me.PropertyPackage.CurrentMaterialStream = form.Collections.CLCS_MaterialStreamCollection(Me.GraphicObject.InputConnectors(0).AttachedConnector.AttachedFrom.Name)

            Select Case Me.FlashType
                Case Helpers.Recycle.FlashType.FlashTP
                    tmp = Me.PropertyPackage.DW_CalcEquilibrio_ISOL(PropertyPackages.FlashSpec.T, PropertyPackages.FlashSpec.P, Tnew, Pnew, 0)
                    'Case Helpers.Recycle.FlashType.FlashPS
                    '    tmp = form.Options.SelectedPropertyPackage.DW_CalcEquilibrio_ISOL(PropertyPackages.FlashSpec.P, PropertyPackages.FlashSpec.S, Pnew, Snew, Tnew)
                    'Case Helpers.Recycle.FlashType.FlashPH
                    '    tmp = form.Options.SelectedPropertyPackage.DW_CalcEquilibrio_ISOL(PropertyPackages.FlashSpec.P, PropertyPackages.FlashSpec.H, Pnew, Hnew, Tnew)
                Case Else
                    tmp = Me.PropertyPackage.DW_CalcEquilibrio_ISOL(PropertyPackages.FlashSpec.T, PropertyPackages.FlashSpec.P, Tnew, Pnew, 0)
            End Select

            'Return New Object() {xl, xv, T, P, H, S, 1, 1, Vx, Vy}
            Dim xl, xv, T, P, H, S, wtotalx, wtotaly As Double
            Dim Vx(ems.Fases(0).Componentes.Count - 1), Vy(ems.Fases(0).Componentes.Count - 1), Vwx(ems.Fases(0).Componentes.Count - 1), Vwy(ems.Fases(0).Componentes.Count - 1) As Double
            xl = tmp(0)
            xv = tmp(1)
            T = tmp(2)
            P = tmp(3)
            H = tmp(4)
            S = tmp(5)
            Vx = tmp(8)
            Vy = tmp(9)

            Dim i As Integer = 0
            Dim j As Integer = 0

            Dim ms As DWSIM.SimulationObjects.Streams.MaterialStream
            Dim cp As ConnectionPoint
            cp = Me.GraphicObject.InputConnectors(0)
            If cp.IsAttached Then
                ms = form.Collections.CLCS_MaterialStreamCollection(cp.AttachedConnector.AttachedFrom.Name)
                Dim comp As DWSIM.ClassesBasicasTermodinamica.Substancia
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
                    .Fases(0).SPMProperties.temperature = Tnew
                    .Fases(0).SPMProperties.pressure = Pnew
                    .Fases(0).SPMProperties.massflow = Wnew
                    .Fases(0).SPMProperties.enthalpy = H
                    Dim comp As DWSIM.ClassesBasicasTermodinamica.Substancia
                    j = 0
                    For Each comp In .Fases(0).Componentes.Values
                        comp.FracaoMolar = ems.Fases(0).Componentes(comp.Nome).FracaoMolar
                        comp.FracaoMassica = ems.Fases(0).Componentes(comp.Nome).FracaoMassica
                        j += 1
                    Next
                    j = 0
                    For Each comp In .Fases(1).Componentes.Values
                        comp.FracaoMolar = Vx(j)
                        comp.FracaoMassica = Vwx(j)
                        j += 1
                    Next
                    j = 0
                    For Each comp In .Fases(2).Componentes.Values
                        comp.FracaoMolar = Vy(j)
                        comp.FracaoMassica = Vwy(j)
                        j += 1
                    Next
                    .Fases(0).SPMProperties.massfraction = 1
                    .Fases(0).SPMProperties.molarfraction = 1
                    .Fases(1).SPMProperties.massfraction = wtotalx
                    .Fases(1).SPMProperties.molarfraction = xl
                    .Fases(2).SPMProperties.massfraction = wtotaly
                    .Fases(2).SPMProperties.molarfraction = xv
                End With
            End If

            If Me.IterationCount >= Me.MaximumIterations Then
                Dim msgres As MsgBoxResult = MessageBox.Show(DWSIM.App.GetLocalString("Onmeromximodeiteraes"), _
                                Me.GraphicObject.Tag & " - " & DWSIM.App.GetLocalString("Nmeromximodeiteraesa3"), _
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                If msgres = MsgBoxResult.No Then
                    GoTo final
                Else
                    Me.IterationCount = 0
                End If
            End If

            Me.IterationCount += 1

            If Math.Abs(Me.ConvergenceHistory.TemperaturaE) > Me.ConvergenceParameters.Temperatura Or _
                Math.Abs(Me.ConvergenceHistory.PressaoE) > Me.ConvergenceParameters.Pressao Or _
                Math.Abs(Me.ConvergenceHistory.VazaoMassicaE) > Me.ConvergenceParameters.VazaoMassica Then

                'Call function to calculate flowsheet
                With objargs
                    .Calculado = True
                    .Nome = Me.Nome
                    .Tipo = TipoObjeto.OT_Reciclo
                End With

                form.CalculationQueue.Enqueue(objargs)

            Else
final:          Me.IterationsTaken = Me.IterationCount.ToString
                Me.IterationCount = 0
            End If

            Me.UpdatePropertyNodes(form.Options.SelectedUnitSystem, form.Options.NumberFormat)

        End Function

        Public Overrides Function DeCalculate() As Integer

            Dim form As Global.DWSIM.FormFlowsheet = Me.Flowsheet

            Me.IterationCount = 0

            If Not Me.GraphicObject.OutputConnectors(0).IsAttached Then
                GoTo final
            End If

            'Dim i As Integer = 0
            'Dim j As Integer = 0

            'Dim ms As DWSIM.SimulationObjects.Streams.MaterialStream
            'Dim cp As ConnectionPoint
            'For Each cp In Me.GraphicObject.OutputConnectors
            '    If cp.IsAttached Then
            '        ms = Form.Collections.CLCS_MaterialStreamCollection(cp.AttachedConnector.AttachedTo.Name)
            '        j = 0
            '        With ms
            '            .Fases(0).SPMProperties.temperature = Nothing
            '            .Fases(0).SPMProperties.pressure = Nothing
            '            .Fases(0).SPMProperties.enthalpy = Nothing
            '            Dim comp As DWSIM.ClassesBasicasTermodinamica.Substancia
            '            For Each comp In .Fases(0).Componentes.Values
            '                comp.FracaoMolar = 0
            '                comp.FracaoMassica = 0
            '                j += 1
            '            Next
            '            .Fases(0).SPMProperties.massflow = Nothing
            '            .Fases(0).SPMProperties.massfraction = 1
            '            .Fases(0).SPMProperties.molarfraction = 1
            '        End With
            '    End If
            '    i += 1
            'Next

final:      'Call function to calculate flowsheet
            'Dim objargs As New DWSIM.Outros.StatusChangeEventArgs
            'With objargs
            '    .Calculado = False
            '    .Nome = Me.Nome
            '    .Tag = Me.GraphicObject.Tag
            '    .Tipo = TipoObjeto.OT_Reciclo
            'End With

            'CalculateFlowsheet(Flowsheet, objargs, Nothing)


        End Function

        Function MAX(ByVal Vv As Object)

            Dim n = UBound(Vv)
            Dim mx As Double

            If n >= 1 Then
                Dim i As Integer = 1
                mx = Vv(i - 1)
                i = 0
                Do
                    If Vv(i) > mx Then
                        mx = Vv(i)
                    End If
                    i += 1
                Loop Until i = n + 1
                Return mx
            Else
                Return Vv(0)
            End If

        End Function

        Public Overrides Sub PopulatePropertyGrid(ByRef pgrid As PropertyGridEx.PropertyGridEx, ByVal su As SistemasDeUnidades.Unidades)

            Dim Conversor As New DWSIM.SistemasDeUnidades.Conversor

            With pgrid

                .PropertySort = PropertySort.Categorized
                .ShowCustomProperties = True
                .Item.Clear()

                Dim ent, saida As String
                If Me.GraphicObject.InputConnectors(0).IsAttached = True Then
                    ent = Me.GraphicObject.InputConnectors(0).AttachedConnector.AttachedFrom.Tag
                Else
                    ent = ""
                End If
                If Me.GraphicObject.OutputConnectors(0).IsAttached = True Then
                    saida = Me.GraphicObject.OutputConnectors(0).AttachedConnector.AttachedTo.Tag
                Else
                    saida = ""
                End If

                .Item.Add(DWSIM.App.GetLocalString("Correntedeentrada"), ent, False, DWSIM.App.GetLocalString("Conexes1"), "", True)
                With .Item(.Item.Count - 1)
                    .DefaultValue = Nothing
                    .CustomEditor = New DWSIM.Editors.Streams.UIInputMSSelector
                End With

                .Item.Add(DWSIM.App.GetLocalString("Correntedesada"), saida, False, DWSIM.App.GetLocalString("Conexes1"), "", True)
                With .Item(.Item.Count - 1)
                    .DefaultValue = Nothing
                    .CustomEditor = New DWSIM.Editors.Streams.UIOutputMSSelector
                End With

                .Item.Add(DWSIM.App.GetLocalString("Mtododeacelerao"), Me, "AccelerationMethod", False, DWSIM.App.GetLocalString("Configuraes2"), DWSIM.App.GetLocalString("Mtododeaceleraodacon"), True)

                If Me.AccelerationMethod = DWSIM.SimulationObjects.SpecialOps.Helpers.Recycle.AccelMethod.Wegstein Then
                    Dim cpc As New CustomPropertyCollection
                    cpc.Add(DWSIM.App.GetLocalString("Atrasonaacelerao"), Me.WegsteinParameters, "AccelDelay", False, DWSIM.App.GetLocalString("ParmetrosWegstein"), "", True)
                    cpc.Add(DWSIM.App.GetLocalString("Ferqunciadeacelerao"), Me.WegsteinParameters, "AccelFreq", False, DWSIM.App.GetLocalString("ParmetrosWegstein"), "", True)
                    cpc.Add(DWSIM.App.GetLocalString("Qmnimo"), Me.WegsteinParameters, "Qmin", False, DWSIM.App.GetLocalString("Wegstein"), "", True)
                    cpc.Add(DWSIM.App.GetLocalString("Qmximo"), Me.WegsteinParameters, "Qmax", False, DWSIM.App.GetLocalString("Wegstein"), "", True)
                    .Item.Add(DWSIM.App.GetLocalString("ParmetrosWegstein"), cpc, True, DWSIM.App.GetLocalString("Configuraes2"), DWSIM.App.GetLocalString("ParmetrosdomtododeWe"))
                    With .Item(.Item.Count - 1)
                        .IsBrowsable = True
                        .BrowsableLabelStyle = BrowsableTypeConverter.LabelStyle.lsEllipsis
                    End With
                End If

                .Item.Add(DWSIM.App.GetLocalString("TipodeFlash"), Me, "FlashType", False, DWSIM.App.GetLocalString("Configuraes2"), DWSIM.App.GetLocalString("Selecioneotipodeclcu"), True)
                .Item.Add(DWSIM.App.GetLocalString("NmeroMximodeIteraes"), Me, "MaximumIterations", False, DWSIM.App.GetLocalString("Configuraes2"), DWSIM.App.GetLocalString("Nmeromximodeiteraesd"), True)

                Dim valor As Double

                valor = Format(Conversor.ConverterDoSI(su.spmp_deltaT, Me.ConvergenceParameters.Temperatura), FlowSheet.Options.NumberFormat)
                .Item.Add(FT(DWSIM.App.GetLocalString("Temperatura"), su.spmp_deltaT), valor, False, DWSIM.App.GetLocalString("Parmetrosdeconvergn3"), DWSIM.App.GetLocalString("Tolernciaparaadifere"), True)
                valor = Format(Conversor.ConverterDoSI(su.spmp_deltaP, Me.ConvergenceParameters.Pressao), FlowSheet.Options.NumberFormat)
                .Item.Add(FT(DWSIM.App.GetLocalString("Presso"), su.spmp_deltaP), valor, False, DWSIM.App.GetLocalString("Parmetrosdeconvergn3"), DWSIM.App.GetLocalString("Tolernciaparaadifere2"), True)
                valor = Format(Conversor.ConverterDoSI(su.spmp_massflow, Me.ConvergenceParameters.VazaoMassica), FlowSheet.Options.NumberFormat)
                .Item.Add(FT(DWSIM.App.GetLocalString("Vazomssica"), su.spmp_massflow), valor, False, DWSIM.App.GetLocalString("Parmetrosdeconvergn3"), DWSIM.App.GetLocalString("Tolernciaparaadifere3"), True)

                .Item.Add(DWSIM.App.GetLocalString("Iteraesnecessrias"), Me, "IterationsTaken", True, DWSIM.App.GetLocalString("Resultados4"), DWSIM.App.GetLocalString("Nmerodeiteraesusadas"), True)
                valor = Format(Conversor.ConverterDoSI(su.spmp_deltaT, Me.ConvergenceHistory.TemperaturaE), FlowSheet.Options.NumberFormat)
                .Item.Add(FT(DWSIM.App.GetLocalString("Erronatemperatura"), su.spmp_deltaT), valor, True, DWSIM.App.GetLocalString("Resultados4"), DWSIM.App.GetLocalString("Diferenaentreosvalor"), True)
                valor = Format(Conversor.ConverterDoSI(su.spmp_deltaP, Me.ConvergenceHistory.PressaoE), FlowSheet.Options.NumberFormat)
                .Item.Add(FT(DWSIM.App.GetLocalString("Erronapresso"), su.spmp_deltaP), valor, True, DWSIM.App.GetLocalString("Resultados4"), DWSIM.App.GetLocalString("Diferenaentreosvalor2"), True)
                valor = Format(Conversor.ConverterDoSI(su.spmp_massflow, Me.ConvergenceHistory.VazaoMassicaE), FlowSheet.Options.NumberFormat)
                .Item.Add(FT(DWSIM.App.GetLocalString("Erronavazomssica"), su.spmp_massflow), valor, True, DWSIM.App.GetLocalString("Resultados4"), DWSIM.App.GetLocalString("Diferenaentreosvalor3"), True)

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
                    'PROP_RY_0	Maximum Iterations
                    value = Me.MaximumIterations
                Case 1
                    'PROP_RY_1	Mass Flow Tolerance
                    value = cv.ConverterDoSI(su.spmp_massflow, Me.ConvergenceParameters.VazaoMassica)
                Case 2
                    'PROP_RY_2	Temperature Tolerance
                    value = cv.ConverterDoSI(su.spmp_temperature, Me.ConvergenceParameters.Temperatura)
                Case 3
                    'PROP_RY_3	Pressure Tolerance
                    value = cv.ConverterDoSI(su.spmp_pressure, Me.ConvergenceParameters.Pressao)
                Case 4
                    'PROP_RY_4	Mass Flow Error
                    value = cv.ConverterDoSI(su.spmp_massflow, Me.ConvergenceHistory.VazaoMassicaE)
                Case 5
                    'PROP_RY_5	Temperature Error
                    value = cv.ConverterDoSI(su.spmp_temperature, Me.ConvergenceHistory.TemperaturaE)
                Case 6
                    'PROP_RY_6	Pressure Error
                    value = cv.ConverterDoSI(su.spmp_pressure, Me.ConvergenceHistory.PressaoE)
            End Select

            Return value



        End Function

        Public Overloads Overrides Function GetProperties(ByVal proptype As SimulationObjects_BaseClass.PropertyType) As String()
            Dim i As Integer = 0
            Dim proplist As New ArrayList
            Select Case proptype
                Case PropertyType.RO
                    For i = 4 To 6
                        proplist.Add("PROP_RY_" + CStr(i))
                    Next
                Case PropertyType.RW
                    For i = 0 To 3
                        proplist.Add("PROP_RY_" + CStr(i))
                    Next
                Case PropertyType.WR
                    For i = 0 To 3
                        proplist.Add("PROP_RY_" + CStr(i))
                    Next
                Case PropertyType.ALL
                    For i = 0 To 6
                        proplist.Add("PROP_RY_" + CStr(i))
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
                    'PROP_RY_0	Maximum Iterations
                    Me.MaximumIterations = propval
                Case 1
                    'PROP_RY_1	Mass Flow Tolerance
                    Me.ConvergenceParameters.VazaoMassica = cv.ConverterParaSI(su.spmp_massflow, propval)
                Case 2
                    'PROP_RY_2	Temperature Tolerance
                    Me.ConvergenceParameters.Temperatura = cv.ConverterParaSI(su.spmp_temperature, propval)
                Case 3
                    'PROP_RY_3	Pressure Tolerance
                    Me.ConvergenceParameters.Pressao = cv.ConverterParaSI(su.spmp_pressure, propval)

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
                    'PROP_RY_0	Maximum Iterations
                    value = ""
                Case 1
                    'PROP_RY_1	Mass Flow Tolerance
                    value = su.spmp_massflow
                Case 2
                    'PROP_RY_2	Temperature Tolerance
                    value = su.spmp_temperature
                Case 3
                    'PROP_RY_3	Pressure Tolerance
                    value = su.spmp_pressure
                Case 4
                    'PROP_RY_4	Mass Flow Error
                    value = su.spmp_massflow
                Case 5
                    'PROP_RY_5	Temperature Error
                    value = su.spmp_deltaT
                Case 6
                    'PROP_RY_6	Pressure Error
                    value = su.spmp_deltaP
            End Select

            Return value

        End Function
    End Class

End Namespace

Namespace DWSIM.SimulationObjects.SpecialOps.Helpers.Recycle

    Public Enum FlashType
        FlashTP
        FlashPH
        FlashPS
    End Enum

    Public Enum AccelMethod
        None
        Wegstein
        Dominant_Eigenvalue
    End Enum

    <System.Serializable()> Public Class ConvergenceParameters

        Implements XMLSerializer.Interfaces.ICustomXMLSerialization

        Public Temperatura As Double = 0.1
        Public Pressao As Double = 0.1
        Public VazaoMassica As Double = 0.01
        Public FracaoVapor As Double = 0.01
        Public Entalpia As Double = 1
        Public Entropia As Double = 0.01
        Public Composicao As Double = 0.001

        Sub New()

        End Sub

        Public Function LoadData(data As System.Collections.Generic.List(Of System.Xml.Linq.XElement)) As Boolean Implements XMLSerializer.Interfaces.ICustomXMLSerialization.LoadData

        End Function

        Public Function SaveData() As System.Collections.Generic.List(Of System.Xml.Linq.XElement) Implements XMLSerializer.Interfaces.ICustomXMLSerialization.SaveData

            Dim elements As New List(Of System.Xml.Linq.XElement)
            Dim ci As Globalization.CultureInfo = Globalization.CultureInfo.InvariantCulture

            With elements

                Dim fields As Reflection.FieldInfo() = Me.GetType.GetFields()
                For Each fi As Reflection.FieldInfo In fields
                    If TypeOf Me.GetType.GetField(fi.Name).FieldType Is IList Then
                        .Add(New XElement(fi.Name, XMLSerializer.XMLSerializer.ArrayToString(Me.GetType.GetField(fi.Name).GetValue(Me), ci)))
                    ElseIf TypeOf Me.GetType.GetField(fi.Name).GetValue(Me) Is Double Then
                        .Add(New XElement(fi.Name, Double.Parse(Me.GetType.GetField(fi.Name).GetValue(Me)).ToString(ci)))
                    Else
                        .Add(New XElement(fi.Name, Me.GetType.GetField(fi.Name).GetValue(Me)))
                    End If
                Next

            End With

            Return elements

        End Function

    End Class

    <System.Serializable()> Public Class ConvergenceHistory

        Implements XMLSerializer.Interfaces.ICustomXMLSerialization

        Public Temperatura As Double = 0
        Public Pressao As Double = 0
        Public VazaoMassica As Double = 0
        Public Entalpia As Double = 0
        Public Entropia As Double = 0
        Public Temperatura0 As Double = 0
        Public Pressao0 As Double = 0
        Public VazaoMassica0 As Double = 0
        Public Entalpia0 As Double = 0
        Public Entropia0 As Double = 0

        Public TemperaturaE As Double = 0
        Public PressaoE As Double = 0
        Public VazaoMassicaE As Double = 0
        Public EntalpiaE As Double = 0
        Public EntropiaE As Double = 0
        Public TemperaturaE0 As Double = 0
        Public PressaoE0 As Double = 0
        Public VazaoMassicaE0 As Double = 0
        Public EntalpiaE0 As Double = 0
        Public EntropiaE0 As Double = 0

        Sub New()

        End Sub

        Public Function LoadData(data As System.Collections.Generic.List(Of System.Xml.Linq.XElement)) As Boolean Implements XMLSerializer.Interfaces.ICustomXMLSerialization.LoadData

        End Function

        Public Function SaveData() As System.Collections.Generic.List(Of System.Xml.Linq.XElement) Implements XMLSerializer.Interfaces.ICustomXMLSerialization.SaveData

            Dim elements As New List(Of System.Xml.Linq.XElement)
            Dim ci As Globalization.CultureInfo = Globalization.CultureInfo.InvariantCulture

            With elements

                Dim fields As Reflection.FieldInfo() = Me.GetType.GetFields()
                For Each fi As Reflection.FieldInfo In fields
                    If TypeOf Me.GetType.GetField(fi.Name).FieldType Is IList Then
                        .Add(New XElement(fi.Name, XMLSerializer.XMLSerializer.ArrayToString(Me.GetType.GetField(fi.Name).GetValue(Me), ci)))
                    ElseIf TypeOf Me.GetType.GetField(fi.Name).GetValue(Me) Is Double Then
                        .Add(New XElement(fi.Name, Double.Parse(Me.GetType.GetField(fi.Name).GetValue(Me)).ToString(ci)))
                    Else
                        .Add(New XElement(fi.Name, Me.GetType.GetField(fi.Name).GetValue(Me)))
                    End If
                Next

            End With

            Return elements

        End Function

    End Class

    <System.Serializable()> Public Class WegsteinParameters

        Public AccelFreq As Integer = 4
        Public Qmax As Double = 0
        Public Qmin As Double = -20
        Public AccelDelay = 2

    End Class

End Namespace

