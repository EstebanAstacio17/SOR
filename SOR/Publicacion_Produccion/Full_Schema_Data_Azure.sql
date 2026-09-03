-- Full Database Script for Azure SQL
SET NOCOUNT ON;
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Almacenes](
	[IdAlmacen] [int] IDENTITY(1,1) NOT NULL,
	[NombreAlmacen] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Direccion] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Responsable] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Telefono] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Activo] [bit] NOT NULL,
	[FechaCreacion] [datetime2](7) NOT NULL,
	[IdUsuarioResponsable] [int] NULL,
	[EsCentral] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdAlmacen] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
SET IDENTITY_INSERT [dbo].[Almacenes] ON 

INSERT [dbo].[Almacenes] ([IdAlmacen], [NombreAlmacen], [Direccion], [Responsable], [Telefono], [Activo], [FechaCreacion], [IdUsuarioResponsable], [EsCentral]) VALUES (1, N'Almacen Central OCC Santo Domingo', N'Av. 27 de Febrero esq. Winston Churchill, Santo Domingo', N'Juan Astacio', N'809-555-0100', 1, CAST(N'2026-09-02T02:48:06.3400000' AS DateTime2), NULL, 1)
INSERT [dbo].[Almacenes] ([IdAlmacen], [NombreAlmacen], [Direccion], [Responsable], [Telefono], [Activo], [FechaCreacion], [IdUsuarioResponsable], [EsCentral]) VALUES (2, N'FUNJEMAR', N'PLAZA FUNJEMAR', N'Efesos Astacio', N'8092323518', 1, CAST(N'2026-09-02T15:28:17.2733333' AS DateTime2), NULL, 1)
SET IDENTITY_INSERT [dbo].[Almacenes] OFF
ALTER TABLE [dbo].[Almacenes] ADD  DEFAULT ((1)) FOR [Activo]
ALTER TABLE [dbo].[Almacenes] ADD  DEFAULT (getdate()) FOR [FechaCreacion]
ALTER TABLE [dbo].[Almacenes] ADD  CONSTRAINT [DF_Almacenes_EsCentral]  DEFAULT ((1)) FOR [EsCentral]

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[AlmacenesEquipos](
	[IdAlmacen] [int] NOT NULL,
	[IdEquipo] [int] NOT NULL,
 CONSTRAINT [PK_AlmacenesEquipos] PRIMARY KEY CLUSTERED 
(
	[IdAlmacen] ASC,
	[IdEquipo] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[AlmacenesEquipos]  WITH CHECK ADD FOREIGN KEY([IdAlmacen])
REFERENCES [dbo].[Almacenes] ([IdAlmacen])
ON DELETE CASCADE
ALTER TABLE [dbo].[AlmacenesEquipos]  WITH CHECK ADD FOREIGN KEY([IdEquipo])
REFERENCES [dbo].[Equipos] ([IdEquipo])
ON DELETE CASCADE

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[AsignacionesEquipo](
	[IdAsignacion] [int] IDENTITY(1,1) NOT NULL,
	[IdUsuario] [int] NOT NULL,
	[IdEquipo] [int] NOT NULL,
	[IdPosicion] [int] NOT NULL,
	[FechaAsignacion] [datetime] NULL,
	[Activo] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdAsignacion] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET IDENTITY_INSERT [dbo].[AsignacionesEquipo] ON 

INSERT [dbo].[AsignacionesEquipo] ([IdAsignacion], [IdUsuario], [IdEquipo], [IdPosicion], [FechaAsignacion], [Activo]) VALUES (6, 1, 2, 3, CAST(N'2026-08-20T19:05:38.830' AS DateTime), 0)
INSERT [dbo].[AsignacionesEquipo] ([IdAsignacion], [IdUsuario], [IdEquipo], [IdPosicion], [FechaAsignacion], [Activo]) VALUES (26, 1, 2, 1, CAST(N'2026-08-24T22:16:04.810' AS DateTime), 0)
INSERT [dbo].[AsignacionesEquipo] ([IdAsignacion], [IdUsuario], [IdEquipo], [IdPosicion], [FechaAsignacion], [Activo]) VALUES (27, 3, 2, 3, CAST(N'2026-08-24T22:16:55.453' AS DateTime), 0)
INSERT [dbo].[AsignacionesEquipo] ([IdAsignacion], [IdUsuario], [IdEquipo], [IdPosicion], [FechaAsignacion], [Activo]) VALUES (28, 3, 2, 3, CAST(N'2026-08-24T22:18:07.297' AS DateTime), 1)
INSERT [dbo].[AsignacionesEquipo] ([IdAsignacion], [IdUsuario], [IdEquipo], [IdPosicion], [FechaAsignacion], [Activo]) VALUES (29, 2, 2, 2, CAST(N'2026-08-24T22:18:12.900' AS DateTime), 1)
INSERT [dbo].[AsignacionesEquipo] ([IdAsignacion], [IdUsuario], [IdEquipo], [IdPosicion], [FechaAsignacion], [Activo]) VALUES (30, 4, 24, 1, CAST(N'2026-09-02T01:16:59.457' AS DateTime), 0)
INSERT [dbo].[AsignacionesEquipo] ([IdAsignacion], [IdUsuario], [IdEquipo], [IdPosicion], [FechaAsignacion], [Activo]) VALUES (31, 4, 24, 1, CAST(N'2026-09-02T01:17:37.690' AS DateTime), 0)
INSERT [dbo].[AsignacionesEquipo] ([IdAsignacion], [IdUsuario], [IdEquipo], [IdPosicion], [FechaAsignacion], [Activo]) VALUES (32, 4, 24, 1, CAST(N'2026-09-02T01:28:31.673' AS DateTime), 1)
INSERT [dbo].[AsignacionesEquipo] ([IdAsignacion], [IdUsuario], [IdEquipo], [IdPosicion], [FechaAsignacion], [Activo]) VALUES (33, 1, 24, 2, CAST(N'2026-09-02T16:56:58.813' AS DateTime), 0)
INSERT [dbo].[AsignacionesEquipo] ([IdAsignacion], [IdUsuario], [IdEquipo], [IdPosicion], [FechaAsignacion], [Activo]) VALUES (34, 1, 24, 3, CAST(N'2026-09-02T17:09:49.990' AS DateTime), 1)
SET IDENTITY_INSERT [dbo].[AsignacionesEquipo] OFF
CREATE UNIQUE NONCLUSTERED INDEX [IX_Asignacion_Equipo_Posicion_Activa] ON [dbo].[AsignacionesEquipo]
(
	[IdEquipo] ASC,
	[IdPosicion] ASC
)
WHERE ([Activo]=(1))
WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[AsignacionesEquipo] ADD  DEFAULT (getdate()) FOR [FechaAsignacion]
ALTER TABLE [dbo].[AsignacionesEquipo] ADD  DEFAULT ((1)) FOR [Activo]
ALTER TABLE [dbo].[AsignacionesEquipo]  WITH CHECK ADD FOREIGN KEY([IdEquipo])
REFERENCES [dbo].[Equipos] ([IdEquipo])
ALTER TABLE [dbo].[AsignacionesEquipo]  WITH CHECK ADD FOREIGN KEY([IdPosicion])
REFERENCES [dbo].[PosicionesOCC] ([IdPosicion])
ALTER TABLE [dbo].[AsignacionesEquipo]  WITH CHECK ADD FOREIGN KEY([IdUsuario])
REFERENCES [dbo].[Usuarios] ([IdUsuario])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[AsignacionesRecursos](
	[IdAsignacionRecurso] [int] IDENTITY(1,1) NOT NULL,
	[IdParticipacion] [int] NOT NULL,
	[OportunidadesEvangelisticas] [int] NULL,
	[LibrosMejorRegalo] [int] NULL,
	[LibrosMaestros] [int] NULL,
	[LibrosAlumno] [int] NULL,
	[Posters] [int] NULL,
	[NuevosTestamentos] [int] NULL,
	[FechaDespacho] [datetime] NULL,
	[IdUsuarioDespacho] [int] NULL,
	[EstadoAsignacion] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[FechaDisponibleDespacho] [datetime2](7) NULL,
	[IdEventoDespachoActual] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdAsignacionRecurso] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
SET IDENTITY_INSERT [dbo].[AsignacionesRecursos] ON 

INSERT [dbo].[AsignacionesRecursos] ([IdAsignacionRecurso], [IdParticipacion], [OportunidadesEvangelisticas], [LibrosMejorRegalo], [LibrosMaestros], [LibrosAlumno], [Posters], [NuevosTestamentos], [FechaDespacho], [IdUsuarioDespacho], [EstadoAsignacion], [FechaDisponibleDespacho], [IdEventoDespachoActual]) VALUES (8, 1, 0, 0, 0, 0, 0, 0, NULL, NULL, N'ASIGNADO', NULL, NULL)
INSERT [dbo].[AsignacionesRecursos] ([IdAsignacionRecurso], [IdParticipacion], [OportunidadesEvangelisticas], [LibrosMejorRegalo], [LibrosMaestros], [LibrosAlumno], [Posters], [NuevosTestamentos], [FechaDespacho], [IdUsuarioDespacho], [EstadoAsignacion], [FechaDisponibleDespacho], [IdEventoDespachoActual]) VALUES (9, 2, 50, 50, 5, 50, 1, 50, NULL, NULL, N'DESPACHADA', CAST(N'2026-09-03T04:36:11.4733333' AS DateTime2), 3)
INSERT [dbo].[AsignacionesRecursos] ([IdAsignacionRecurso], [IdParticipacion], [OportunidadesEvangelisticas], [LibrosMejorRegalo], [LibrosMaestros], [LibrosAlumno], [Posters], [NuevosTestamentos], [FechaDespacho], [IdUsuarioDespacho], [EstadoAsignacion], [FechaDisponibleDespacho], [IdEventoDespachoActual]) VALUES (10, 3, 50, 50, 1, 30, 1, 20, NULL, NULL, N'DESPACHADA', CAST(N'2026-09-02T19:29:54.0300000' AS DateTime2), 9)
SET IDENTITY_INSERT [dbo].[AsignacionesRecursos] OFF
CREATE NONCLUSTERED INDEX [IX_AsignacionesRecursos_IdPart] ON [dbo].[AsignacionesRecursos]
(
	[IdParticipacion] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[AsignacionesRecursos] ADD  DEFAULT ((0)) FOR [OportunidadesEvangelisticas]
ALTER TABLE [dbo].[AsignacionesRecursos] ADD  DEFAULT ((0)) FOR [LibrosMejorRegalo]
ALTER TABLE [dbo].[AsignacionesRecursos] ADD  DEFAULT ((0)) FOR [LibrosMaestros]
ALTER TABLE [dbo].[AsignacionesRecursos] ADD  DEFAULT ((0)) FOR [LibrosAlumno]
ALTER TABLE [dbo].[AsignacionesRecursos] ADD  DEFAULT ((0)) FOR [Posters]
ALTER TABLE [dbo].[AsignacionesRecursos] ADD  DEFAULT ((0)) FOR [NuevosTestamentos]
ALTER TABLE [dbo].[AsignacionesRecursos] ADD  DEFAULT ('ASIGNADO') FOR [EstadoAsignacion]
ALTER TABLE [dbo].[AsignacionesRecursos]  WITH CHECK ADD FOREIGN KEY([IdParticipacion])
REFERENCES [dbo].[ParticipacionesIglesia] ([IdParticipacion])
ALTER TABLE [dbo].[AsignacionesRecursos]  WITH CHECK ADD FOREIGN KEY([IdUsuarioDespacho])
REFERENCES [dbo].[Usuarios] ([IdUsuario])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[AsistenciaMaestro](
	[IdAsistencia] [int] IDENTITY(1,1) NOT NULL,
	[IdMaestro] [int] NOT NULL,
	[IdEvento] [int] NOT NULL,
	[Asistio] [bit] NULL,
	[FechaRegistro] [datetime] NULL,
	[IdUsuarioRegistro] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdAsistencia] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

CREATE UNIQUE NONCLUSTERED INDEX [UQ_AsistenciaMaestro_Evento_Maestro] ON [dbo].[AsistenciaMaestro]
(
	[IdEvento] ASC,
	[IdMaestro] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[AsistenciaMaestro] ADD  DEFAULT ((0)) FOR [Asistio]
ALTER TABLE [dbo].[AsistenciaMaestro] ADD  DEFAULT (getdate()) FOR [FechaRegistro]
ALTER TABLE [dbo].[AsistenciaMaestro]  WITH CHECK ADD FOREIGN KEY([IdEvento])
REFERENCES [dbo].[Eventos] ([IdEvento])
ALTER TABLE [dbo].[AsistenciaMaestro]  WITH CHECK ADD FOREIGN KEY([IdMaestro])
REFERENCES [dbo].[Maestros] ([IdMaestro])
ALTER TABLE [dbo].[AsistenciaMaestro]  WITH CHECK ADD FOREIGN KEY([IdUsuarioRegistro])
REFERENCES [dbo].[Usuarios] ([IdUsuario])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[AuditoriaGeneral](
	[IdAuditoria] [bigint] IDENTITY(1,1) NOT NULL,
	[FechaHora] [datetime2](7) NULL,
	[IdUsuario] [int] NULL,
	[CorreoUsuario] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Accion] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Modulo] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[IdRegistroAfectado] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Detalles] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[DireccionIP] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[IdAuditoria] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
SET IDENTITY_INSERT [dbo].[AuditoriaGeneral] ON 

INSERT [dbo].[AuditoriaGeneral] ([IdAuditoria], [FechaHora], [IdUsuario], [CorreoUsuario], [Accion], [Modulo], [IdRegistroAfectado], [Detalles], [DireccionIP]) VALUES (1, CAST(N'2026-09-02T01:16:59.4600000' AS DateTime2), 1, N'admin@occrd.org', N'EDITAR_USUARIO', N'ADMINISTRACION_USUARIOS', N'4', N'Edición de usuario #4 (cm@erle.com): Rol anterior: 3 -> Rol nuevo: 3, Estado anterior: 4 -> Estado nuevo: 4.', N'::1')
INSERT [dbo].[AuditoriaGeneral] ([IdAuditoria], [FechaHora], [IdUsuario], [CorreoUsuario], [Accion], [Modulo], [IdRegistroAfectado], [Detalles], [DireccionIP]) VALUES (2, CAST(N'2026-09-02T01:17:37.6933333' AS DateTime2), 1, N'admin@occrd.org', N'EDITAR_USUARIO', N'ADMINISTRACION_USUARIOS', N'4', N'Edición de usuario #4 (cm@erle.com): Rol anterior: 3 -> Rol nuevo: 3, Estado anterior: 4 -> Estado nuevo: 4.', N'::1')
INSERT [dbo].[AuditoriaGeneral] ([IdAuditoria], [FechaHora], [IdUsuario], [CorreoUsuario], [Accion], [Modulo], [IdRegistroAfectado], [Detalles], [DireccionIP]) VALUES (3, CAST(N'2026-09-02T01:20:53.4233333' AS DateTime2), 1, N'admin@occrd.org', N'EDITAR_USUARIO', N'ADMINISTRACION_USUARIOS', N'4', N'Edición de usuario #4 (cm@erle.com): Rol anterior: 3 -> Rol nuevo: 3, Estado anterior: 4 -> Estado nuevo: 3.', N'::1')
INSERT [dbo].[AuditoriaGeneral] ([IdAuditoria], [FechaHora], [IdUsuario], [CorreoUsuario], [Accion], [Modulo], [IdRegistroAfectado], [Detalles], [DireccionIP]) VALUES (4, CAST(N'2026-09-02T05:13:33.1233333' AS DateTime2), 1, N'admin@occrd.org', N'UPDATE', N'Iglesia', N'2', N'Edición de iglesia: Iglesia Un Templo', N'::1')
INSERT [dbo].[AuditoriaGeneral] ([IdAuditoria], [FechaHora], [IdUsuario], [CorreoUsuario], [Accion], [Modulo], [IdRegistroAfectado], [Detalles], [DireccionIP]) VALUES (5, CAST(N'2026-09-02T15:29:30.6433333' AS DateTime2), 1, NULL, N'Recepción Contenedor', N'Logistica', N'6', N'Contenedor CONT PRUEBA 001 registrado con 1 materiales.', N'::1')
INSERT [dbo].[AuditoriaGeneral] ([IdAuditoria], [FechaHora], [IdUsuario], [CorreoUsuario], [Accion], [Modulo], [IdRegistroAfectado], [Detalles], [DireccionIP]) VALUES (6, CAST(N'2026-09-02T16:48:56.4400000' AS DateTime2), 1, NULL, N'Recepción Contenedor', N'Logistica', N'7', N'Contenedor TONT PRUEBA recibido y confirmado exitosamente en almacén ID 2. Total materiales: 1.', N'::1')
INSERT [dbo].[AuditoriaGeneral] ([IdAuditoria], [FechaHora], [IdUsuario], [CorreoUsuario], [Accion], [Modulo], [IdRegistroAfectado], [Detalles], [DireccionIP]) VALUES (7, CAST(N'2026-09-02T16:56:58.8133333' AS DateTime2), 1, N'admin@occrd.org', N'EDITAR_SUPERADMIN', N'ADMINISTRACION_USUARIOS', N'1', N'Edición de usuario #1 (admin@occrd.org): Rol anterior: 1 -> Rol nuevo: 1, Estado anterior: 4 -> Estado nuevo: 4.', N'::1')
INSERT [dbo].[AuditoriaGeneral] ([IdAuditoria], [FechaHora], [IdUsuario], [CorreoUsuario], [Accion], [Modulo], [IdRegistroAfectado], [Detalles], [DireccionIP]) VALUES (8, CAST(N'2026-09-02T17:09:49.9933333' AS DateTime2), 1, N'admin@occrd.org', N'EDITAR_SUPERADMIN', N'ADMINISTRACION_USUARIOS', N'1', N'Edición de usuario #1 (admin@occrd.org): Rol anterior: 1 -> Rol nuevo: 1, Estado anterior: 4 -> Estado nuevo: 4.', N'::1')
INSERT [dbo].[AuditoriaGeneral] ([IdAuditoria], [FechaHora], [IdUsuario], [CorreoUsuario], [Accion], [Modulo], [IdRegistroAfectado], [Detalles], [DireccionIP]) VALUES (9, CAST(N'2026-09-02T18:34:50.6600000' AS DateTime2), 1, NULL, N'Recepción Contenedor', N'Logistica', N'8', N'Contenedor CONT PRUEBA 050 recibido y confirmado exitosamente en almacén ID 2. Total materiales: 1.', N'::1')
INSERT [dbo].[AuditoriaGeneral] ([IdAuditoria], [FechaHora], [IdUsuario], [CorreoUsuario], [Accion], [Modulo], [IdRegistroAfectado], [Detalles], [DireccionIP]) VALUES (10, CAST(N'2026-09-02T19:29:54.0366667' AS DateTime2), 1, NULL, N'Despacho Confirmado', N'Logistica', N'6', N'Iglesia ID 3 despachada. Receptor: Servicio Al Cliente y Servicio Al Cliente (AMBOS).', N'::1')
INSERT [dbo].[AuditoriaGeneral] ([IdAuditoria], [FechaHora], [IdUsuario], [CorreoUsuario], [Accion], [Modulo], [IdRegistroAfectado], [Detalles], [DireccionIP]) VALUES (11, CAST(N'2026-09-02T19:46:05.6766667' AS DateTime2), 1, NULL, N'Transferencia Equipo', N'Logistica', N'5', N'Transferencia TRF-20260902-6385 registrada con éxito. Estado: EMITIDA.', N'::1')
INSERT [dbo].[AuditoriaGeneral] ([IdAuditoria], [FechaHora], [IdUsuario], [CorreoUsuario], [Accion], [Modulo], [IdRegistroAfectado], [Detalles], [DireccionIP]) VALUES (12, CAST(N'2026-09-02T19:46:56.9666667' AS DateTime2), 1, NULL, N'Confirmar Recepción', N'Logistica', N'5', N'Transferencia TRF-20260902-6385 confirmada como RECIBIDA por CMI Rol.', N'::1')
INSERT [dbo].[AuditoriaGeneral] ([IdAuditoria], [FechaHora], [IdUsuario], [CorreoUsuario], [Accion], [Modulo], [IdRegistroAfectado], [Detalles], [DireccionIP]) VALUES (13, CAST(N'2026-09-02T19:55:20.2666667' AS DateTime2), 1, NULL, N'Transferencia Equipo', N'Logistica', N'6', N'Transferencia TRF-20260902-2659 registrada con éxito. Estado: EMITIDA.', N'::1')
INSERT [dbo].[AuditoriaGeneral] ([IdAuditoria], [FechaHora], [IdUsuario], [CorreoUsuario], [Accion], [Modulo], [IdRegistroAfectado], [Detalles], [DireccionIP]) VALUES (14, CAST(N'2026-09-02T19:55:45.4500000' AS DateTime2), 1, NULL, N'Confirmar Recepción', N'Logistica', N'6', N'Transferencia TRF-20260902-2659 confirmada como RECIBIDA por CD Rol.', N'::1')
INSERT [dbo].[AuditoriaGeneral] ([IdAuditoria], [FechaHora], [IdUsuario], [CorreoUsuario], [Accion], [Modulo], [IdRegistroAfectado], [Detalles], [DireccionIP]) VALUES (15, CAST(N'2026-09-03T04:36:11.4766667' AS DateTime2), 1, NULL, N'Despacho Confirmado', N'Logistica', N'5', N'Iglesia ID 2 despachada. Receptor: Pedro Picaso y Maria Parlanchina (AMBOS).', N'::1')
SET IDENTITY_INSERT [dbo].[AuditoriaGeneral] OFF
SET ANSI_PADDING ON

CREATE NONCLUSTERED INDEX [IX_AuditoriaGeneral_Modulo_Fecha] ON [dbo].[AuditoriaGeneral]
(
	[Modulo] ASC,
	[FechaHora] DESC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[AuditoriaGeneral] ADD  DEFAULT (getdate()) FOR [FechaHora]

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[BitacoraLlamadasAcompanamiento](
	[IdLlamada] [int] IDENTITY(1,1) NOT NULL,
	[IdParticipacion] [int] NOT NULL,
	[IdIglesia] [int] NOT NULL,
	[FechaHora] [datetime] NOT NULL,
	[IdUsuarioCoordinador] [int] NULL,
	[NombreCoordinador] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[EtapaDiscipulado] [nvarchar](80) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ObstaculoReportado] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ApoyoRequerido] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AccionAcordada] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[SemaforoEstado] [nvarchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[EnfoqueAplicado] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[DuracionMinutos] [int] NOT NULL,
	[FechaRegistro] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdLlamada] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

CREATE NONCLUSTERED INDEX [IX_BitacoraLlamadas_Iglesia] ON [dbo].[BitacoraLlamadasAcompanamiento]
(
	[IdIglesia] ASC,
	[FechaHora] DESC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_BitacoraLlamadas_Part] ON [dbo].[BitacoraLlamadasAcompanamiento]
(
	[IdParticipacion] ASC,
	[FechaHora] DESC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[BitacoraLlamadasAcompanamiento] ADD  DEFAULT (getdate()) FOR [FechaHora]
ALTER TABLE [dbo].[BitacoraLlamadasAcompanamiento] ADD  DEFAULT ('VERDE') FOR [SemaforoEstado]
ALTER TABLE [dbo].[BitacoraLlamadasAcompanamiento] ADD  DEFAULT ('EQUILIBRIO') FOR [EnfoqueAplicado]
ALTER TABLE [dbo].[BitacoraLlamadasAcompanamiento] ADD  DEFAULT ((5)) FOR [DuracionMinutos]
ALTER TABLE [dbo].[BitacoraLlamadasAcompanamiento] ADD  DEFAULT (getdate()) FOR [FechaRegistro]

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[ComentariosObservaciones](
	[IdComentario] [int] IDENTITY(1,1) NOT NULL,
	[IdIglesia] [int] NOT NULL,
	[IdUsuario] [int] NOT NULL,
	[Comentario] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[FechaCreacion] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdComentario] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET IDENTITY_INSERT [dbo].[ComentariosObservaciones] ON 

INSERT [dbo].[ComentariosObservaciones] ([IdComentario], [IdIglesia], [IdUsuario], [Comentario], [FechaCreacion]) VALUES (2, 2, 2, N'Uncomentario', CAST(N'2026-08-24T22:26:37.167' AS DateTime))
INSERT [dbo].[ComentariosObservaciones] ([IdComentario], [IdIglesia], [IdUsuario], [Comentario], [FechaCreacion]) VALUES (3, 2, 3, N'Maestro Eliminado: Maria LA Del Barrio. Razón: cambio deamesto', CAST(N'2026-08-24T23:39:56.793' AS DateTime))
SET IDENTITY_INSERT [dbo].[ComentariosObservaciones] OFF
CREATE NONCLUSTERED INDEX [IX_Comentarios_IdIglesia] ON [dbo].[ComentariosObservaciones]
(
	[IdIglesia] ASC,
	[FechaCreacion] DESC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[ComentariosObservaciones] ADD  DEFAULT (getdate()) FOR [FechaCreacion]
ALTER TABLE [dbo].[ComentariosObservaciones]  WITH CHECK ADD FOREIGN KEY([IdIglesia])
REFERENCES [dbo].[Iglesias] ([IdIglesia])
ALTER TABLE [dbo].[ComentariosObservaciones]  WITH CHECK ADD FOREIGN KEY([IdUsuario])
REFERENCES [dbo].[Usuarios] ([IdUsuario])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[CompanerosOracion](
	[IdCompanero] [int] IDENTITY(1,1) NOT NULL,
	[NombreCompleto] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ContactoWhatsApp] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[EsMayorEdad] [bit] NULL,
	[IdIglesia] [int] NOT NULL,
	[IdTemporada] [int] NOT NULL,
	[IdUsuarioRegistro] [int] NOT NULL,
	[FechaRegistro] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdCompanero] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
SET IDENTITY_INSERT [dbo].[CompanerosOracion] ON 

INSERT [dbo].[CompanerosOracion] ([IdCompanero], [NombreCompleto], [ContactoWhatsApp], [EsMayorEdad], [IdIglesia], [IdTemporada], [IdUsuarioRegistro], [FechaRegistro]) VALUES (0, N'Maria Altagracia', N'8092323518', 1, 2, 1, 1, CAST(N'2026-09-03T03:41:30.040' AS DateTime))
SET IDENTITY_INSERT [dbo].[CompanerosOracion] OFF
ALTER TABLE [dbo].[CompanerosOracion] ADD  DEFAULT ((1)) FOR [EsMayorEdad]
ALTER TABLE [dbo].[CompanerosOracion] ADD  DEFAULT (getdate()) FOR [FechaRegistro]
ALTER TABLE [dbo].[CompanerosOracion]  WITH CHECK ADD FOREIGN KEY([IdIglesia])
REFERENCES [dbo].[Iglesias] ([IdIglesia])
ALTER TABLE [dbo].[CompanerosOracion]  WITH CHECK ADD FOREIGN KEY([IdTemporada])
REFERENCES [dbo].[Temporadas] ([IdTemporada])
ALTER TABLE [dbo].[CompanerosOracion]  WITH CHECK ADD FOREIGN KEY([IdUsuarioRegistro])
REFERENCES [dbo].[Usuarios] ([IdUsuario])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[ConfiguracionesSistema](
	[Clave] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Valor] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Clave] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
INSERT [dbo].[ConfiguracionesSistema] ([Clave], [Valor]) VALUES (N'MinAniosAntiguedad', N'3')

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[CoordinadoresEventoDespacho](
	[IdCoordinadorEvento] [int] IDENTITY(1,1) NOT NULL,
	[IdEvento] [int] NOT NULL,
	[IdUsuario] [int] NOT NULL,
	[HoraEntrada] [time](7) NULL,
	[HoraSalida] [time](7) NULL,
	[Presente] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdCoordinadorEvento] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[CoordinadoresEventoDespacho] ADD  CONSTRAINT [UQ_Coordinador_Evento] UNIQUE NONCLUSTERED 
(
	[IdEvento] ASC,
	[IdUsuario] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[CoordinadoresEventoDespacho] ADD  DEFAULT ((1)) FOR [Presente]
ALTER TABLE [dbo].[CoordinadoresEventoDespacho]  WITH CHECK ADD FOREIGN KEY([IdEvento])
REFERENCES [dbo].[Eventos] ([IdEvento])
ALTER TABLE [dbo].[CoordinadoresEventoDespacho]  WITH CHECK ADD FOREIGN KEY([IdUsuario])
REFERENCES [dbo].[Usuarios] ([IdUsuario])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[Denominaciones](
	[IdDenominacion] [int] IDENTITY(1,1) NOT NULL,
	[Nombre] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Activo] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdDenominacion] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET IDENTITY_INSERT [dbo].[Denominaciones] ON 

INSERT [dbo].[Denominaciones] ([IdDenominacion], [Nombre], [Activo]) VALUES (1, N'Asambleas de Dios', 1)
INSERT [dbo].[Denominaciones] ([IdDenominacion], [Nombre], [Activo]) VALUES (2, N'Bautista', 1)
INSERT [dbo].[Denominaciones] ([IdDenominacion], [Nombre], [Activo]) VALUES (3, N'Metodista', 1)
INSERT [dbo].[Denominaciones] ([IdDenominacion], [Nombre], [Activo]) VALUES (4, N'Iglesia de Dios', 1)
INSERT [dbo].[Denominaciones] ([IdDenominacion], [Nombre], [Activo]) VALUES (5, N'Pentecostal', 1)
INSERT [dbo].[Denominaciones] ([IdDenominacion], [Nombre], [Activo]) VALUES (6, N'Independiente / No Denominacional', 1)
INSERT [dbo].[Denominaciones] ([IdDenominacion], [Nombre], [Activo]) VALUES (7, N'Alianza Cristiana y Misionera', 1)
SET IDENTITY_INSERT [dbo].[Denominaciones] OFF
ALTER TABLE [dbo].[Denominaciones] ADD  DEFAULT ((1)) FOR [Activo]

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[DespachosIglesia](
	[IdDespachoIglesia] [int] IDENTITY(1,1) NOT NULL,
	[NumeroComprobanteDespacho] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[IdEvento] [int] NOT NULL,
	[IdParticipacion] [int] NOT NULL,
	[IdIglesia] [int] NOT NULL,
	[IdTemporada] [int] NOT NULL,
	[IdEquipo] [int] NOT NULL,
	[EstadoDespacho] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[TipoReceptor] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[NombreReceptor] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[DocumentoIdentidadReceptor] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TelefonoReceptor] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FechaHoraEntrega] [datetime2](7) NULL,
	[CoordinadorDespachador] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IdUsuarioDespacho] [int] NULL,
	[MotivoNoDespacho] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Observaciones] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[EvidenciaRuta] [varchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FechaRegistro] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdDespachoIglesia] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
SET IDENTITY_INSERT [dbo].[DespachosIglesia] ON 

INSERT [dbo].[DespachosIglesia] ([IdDespachoIglesia], [NumeroComprobanteDespacho], [IdEvento], [IdParticipacion], [IdIglesia], [IdTemporada], [IdEquipo], [EstadoDespacho], [TipoReceptor], [NombreReceptor], [DocumentoIdentidadReceptor], [TelefonoReceptor], [FechaHoraEntrega], [CoordinadorDespachador], [IdUsuarioDespacho], [MotivoNoDespacho], [Observaciones], [EvidenciaRuta], [FechaRegistro]) VALUES (2, N'DSP-TEST-8363', 4, 2, 2, 1, 2, N'DESPACHADA', N'PASTOR', N'Pastor Juan PÃ©rez Test', N'001-0123456-7', N'809-555-9988', CAST(N'2026-09-02T02:55:12.2733333' AS DateTime2), N'Coordinador Entrega', 1, NULL, NULL, NULL, CAST(N'2026-09-02T02:55:12.2466667' AS DateTime2))
INSERT [dbo].[DespachosIglesia] ([IdDespachoIglesia], [NumeroComprobanteDespacho], [IdEvento], [IdParticipacion], [IdIglesia], [IdTemporada], [IdEquipo], [EstadoDespacho], [TipoReceptor], [NombreReceptor], [DocumentoIdentidadReceptor], [TelefonoReceptor], [FechaHoraEntrega], [CoordinadorDespachador], [IdUsuarioDespacho], [MotivoNoDespacho], [Observaciones], [EvidenciaRuta], [FechaRegistro]) VALUES (3, N'DSP-NODESP-2947', 4, 2, 2, 1, 2, N'NO_DESPACHADA', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'Pastor no asistiÃ³ al evento', NULL, NULL, CAST(N'2026-09-02T02:55:12.2866667' AS DateTime2))
INSERT [dbo].[DespachosIglesia] ([IdDespachoIglesia], [NumeroComprobanteDespacho], [IdEvento], [IdParticipacion], [IdIglesia], [IdTemporada], [IdEquipo], [EstadoDespacho], [TipoReceptor], [NombreReceptor], [DocumentoIdentidadReceptor], [TelefonoReceptor], [FechaHoraEntrega], [CoordinadorDespachador], [IdUsuarioDespacho], [MotivoNoDespacho], [Observaciones], [EvidenciaRuta], [FechaRegistro]) VALUES (4, N'DSP-TEST-7315', 5, 2, 2, 1, 1, N'DESPACHADA', N'PASTOR', N'Pastor Principal Test', N'001-0000000-1', NULL, CAST(N'2026-09-02T03:30:13.3400000' AS DateTime2), N'Coordinador Oficial', 1, NULL, NULL, NULL, CAST(N'2026-09-02T03:30:13.3166667' AS DateTime2))
INSERT [dbo].[DespachosIglesia] ([IdDespachoIglesia], [NumeroComprobanteDespacho], [IdEvento], [IdParticipacion], [IdIglesia], [IdTemporada], [IdEquipo], [EstadoDespacho], [TipoReceptor], [NombreReceptor], [DocumentoIdentidadReceptor], [TelefonoReceptor], [FechaHoraEntrega], [CoordinadorDespachador], [IdUsuarioDespacho], [MotivoNoDespacho], [Observaciones], [EvidenciaRuta], [FechaRegistro]) VALUES (5, N'DSP-20260902-6937', 3, 2, 2, 1, 2, N'DESPACHADA', N'AMBOS', N'Pedro Picaso y Maria Parlanchina', N'40224472406 / 40258789654', N'8295656565 • 8497898523', CAST(N'2026-09-03T04:36:11.4733333' AS DateTime2), N'Juan Astacio', 1, NULL, NULL, NULL, CAST(N'2026-09-02T04:52:00.8033333' AS DateTime2))
INSERT [dbo].[DespachosIglesia] ([IdDespachoIglesia], [NumeroComprobanteDespacho], [IdEvento], [IdParticipacion], [IdIglesia], [IdTemporada], [IdEquipo], [EstadoDespacho], [TipoReceptor], [NombreReceptor], [DocumentoIdentidadReceptor], [TelefonoReceptor], [FechaHoraEntrega], [CoordinadorDespachador], [IdUsuarioDespacho], [MotivoNoDespacho], [Observaciones], [EvidenciaRuta], [FechaRegistro]) VALUES (6, N'DSP-20260902-5949', 9, 3, 3, 1, 24, N'DESPACHADA', N'AMBOS', N'Servicio Al Cliente y Servicio Al Cliente', N'40224442406 / 40145856587', N'8295656565 • 8497898523', CAST(N'2026-09-02T19:29:54.0300000' AS DateTime2), N'Juan Astacio', 1, NULL, NULL, NULL, CAST(N'2026-09-02T18:03:49.0666667' AS DateTime2))
SET IDENTITY_INSERT [dbo].[DespachosIglesia] OFF
SET ANSI_PADDING ON

ALTER TABLE [dbo].[DespachosIglesia] ADD UNIQUE NONCLUSTERED 
(
	[NumeroComprobanteDespacho] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[DespachosIglesia] ADD  DEFAULT ('PROGRAMADA') FOR [EstadoDespacho]
ALTER TABLE [dbo].[DespachosIglesia] ADD  DEFAULT (getdate()) FOR [FechaRegistro]
ALTER TABLE [dbo].[DespachosIglesia]  WITH CHECK ADD FOREIGN KEY([IdEquipo])
REFERENCES [dbo].[Equipos] ([IdEquipo])
ALTER TABLE [dbo].[DespachosIglesia]  WITH CHECK ADD FOREIGN KEY([IdEvento])
REFERENCES [dbo].[Eventos] ([IdEvento])
ALTER TABLE [dbo].[DespachosIglesia]  WITH CHECK ADD FOREIGN KEY([IdIglesia])
REFERENCES [dbo].[Iglesias] ([IdIglesia])
ALTER TABLE [dbo].[DespachosIglesia]  WITH CHECK ADD FOREIGN KEY([IdParticipacion])
REFERENCES [dbo].[ParticipacionesIglesia] ([IdParticipacion])
ALTER TABLE [dbo].[DespachosIglesia]  WITH CHECK ADD FOREIGN KEY([IdTemporada])
REFERENCES [dbo].[Temporadas] ([IdTemporada])
ALTER TABLE [dbo].[DespachosIglesia]  WITH CHECK ADD FOREIGN KEY([IdUsuarioDespacho])
REFERENCES [dbo].[Usuarios] ([IdUsuario])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[DespachosIglesiaDetalle](
	[IdDespachoDetalle] [int] IDENTITY(1,1) NOT NULL,
	[IdDespachoIglesia] [int] NOT NULL,
	[IdMaterial] [int] NOT NULL,
	[CantidadAsignada] [int] NOT NULL,
	[CantidadDespachada] [int] NOT NULL,
	[CantidadNoDespachada]  AS ([CantidadAsignada]-[CantidadDespachada]) PERSISTED,
PRIMARY KEY CLUSTERED 
(
	[IdDespachoDetalle] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
SET ANSI_PADDING ON
SET IDENTITY_INSERT [dbo].[DespachosIglesiaDetalle] ON 

INSERT [dbo].[DespachosIglesiaDetalle] ([IdDespachoDetalle], [IdDespachoIglesia], [IdMaterial], [CantidadAsignada], [CantidadDespachada]) VALUES (1, 2, 1, 10, 10)
INSERT [dbo].[DespachosIglesiaDetalle] ([IdDespachoDetalle], [IdDespachoIglesia], [IdMaterial], [CantidadAsignada], [CantidadDespachada]) VALUES (2, 4, 3, 50, 50)
INSERT [dbo].[DespachosIglesiaDetalle] ([IdDespachoDetalle], [IdDespachoIglesia], [IdMaterial], [CantidadAsignada], [CantidadDespachada]) VALUES (3, 5, 3, 50, 50)
INSERT [dbo].[DespachosIglesiaDetalle] ([IdDespachoDetalle], [IdDespachoIglesia], [IdMaterial], [CantidadAsignada], [CantidadDespachada]) VALUES (4, 5, 4, 50, 0)
INSERT [dbo].[DespachosIglesiaDetalle] ([IdDespachoDetalle], [IdDespachoIglesia], [IdMaterial], [CantidadAsignada], [CantidadDespachada]) VALUES (5, 5, 5, 1, 0)
INSERT [dbo].[DespachosIglesiaDetalle] ([IdDespachoDetalle], [IdDespachoIglesia], [IdMaterial], [CantidadAsignada], [CantidadDespachada]) VALUES (6, 5, 6, 50, 0)
INSERT [dbo].[DespachosIglesiaDetalle] ([IdDespachoDetalle], [IdDespachoIglesia], [IdMaterial], [CantidadAsignada], [CantidadDespachada]) VALUES (7, 6, 3, 50, 50)
INSERT [dbo].[DespachosIglesiaDetalle] ([IdDespachoDetalle], [IdDespachoIglesia], [IdMaterial], [CantidadAsignada], [CantidadDespachada]) VALUES (8, 6, 4, 50, 0)
INSERT [dbo].[DespachosIglesiaDetalle] ([IdDespachoDetalle], [IdDespachoIglesia], [IdMaterial], [CantidadAsignada], [CantidadDespachada]) VALUES (9, 6, 5, 1, 0)
INSERT [dbo].[DespachosIglesiaDetalle] ([IdDespachoDetalle], [IdDespachoIglesia], [IdMaterial], [CantidadAsignada], [CantidadDespachada]) VALUES (10, 6, 6, 20, 0)
INSERT [dbo].[DespachosIglesiaDetalle] ([IdDespachoDetalle], [IdDespachoIglesia], [IdMaterial], [CantidadAsignada], [CantidadDespachada]) VALUES (11, 6, 2, 30, 0)
INSERT [dbo].[DespachosIglesiaDetalle] ([IdDespachoDetalle], [IdDespachoIglesia], [IdMaterial], [CantidadAsignada], [CantidadDespachada]) VALUES (12, 6, 1, 1, 0)
INSERT [dbo].[DespachosIglesiaDetalle] ([IdDespachoDetalle], [IdDespachoIglesia], [IdMaterial], [CantidadAsignada], [CantidadDespachada]) VALUES (13, 5, 2, 50, 0)
INSERT [dbo].[DespachosIglesiaDetalle] ([IdDespachoDetalle], [IdDespachoIglesia], [IdMaterial], [CantidadAsignada], [CantidadDespachada]) VALUES (14, 5, 1, 5, 0)
SET IDENTITY_INSERT [dbo].[DespachosIglesiaDetalle] OFF
SET ANSI_PADDING OFF
ALTER TABLE [dbo].[DespachosIglesiaDetalle] ADD  DEFAULT ((0)) FOR [CantidadDespachada]
ALTER TABLE [dbo].[DespachosIglesiaDetalle]  WITH CHECK ADD FOREIGN KEY([IdDespachoIglesia])
REFERENCES [dbo].[DespachosIglesia] ([IdDespachoIglesia])
ALTER TABLE [dbo].[DespachosIglesiaDetalle]  WITH CHECK ADD FOREIGN KEY([IdMaterial])
REFERENCES [dbo].[Materiales] ([IdMaterial])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[EOS_GruposNoAlcanzados](
	[IdGNA] [int] IDENTITY(1,1) NOT NULL,
	[IdTemporada] [int] NOT NULL,
	[IdEquipo] [int] NOT NULL,
	[NombreGNA] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CompaneroMinisterio] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CajitasEntregadas] [int] NOT NULL,
	[InscritosLGA] [int] NOT NULL,
	[NinosCreenJesus] [int] NOT NULL,
	[NinosOranComparten] [int] NOT NULL,
	[NinosGraduados] [int] NOT NULL,
	[Notas] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FechaCreacion] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdGNA] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[EOS_GruposNoAlcanzados] ADD  DEFAULT ((0)) FOR [CajitasEntregadas]
ALTER TABLE [dbo].[EOS_GruposNoAlcanzados] ADD  DEFAULT ((0)) FOR [InscritosLGA]
ALTER TABLE [dbo].[EOS_GruposNoAlcanzados] ADD  DEFAULT ((0)) FOR [NinosCreenJesus]
ALTER TABLE [dbo].[EOS_GruposNoAlcanzados] ADD  DEFAULT ((0)) FOR [NinosOranComparten]
ALTER TABLE [dbo].[EOS_GruposNoAlcanzados] ADD  DEFAULT ((0)) FOR [NinosGraduados]
ALTER TABLE [dbo].[EOS_GruposNoAlcanzados] ADD  DEFAULT (getdate()) FOR [FechaCreacion]
ALTER TABLE [dbo].[EOS_GruposNoAlcanzados]  WITH CHECK ADD FOREIGN KEY([IdEquipo])
REFERENCES [dbo].[Equipos] ([IdEquipo])
ALTER TABLE [dbo].[EOS_GruposNoAlcanzados]  WITH CHECK ADD FOREIGN KEY([IdTemporada])
REFERENCES [dbo].[Temporadas] ([IdTemporada])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[EOS_IglesiasPlantadas](
	[IdIglesiaPlantada] [int] IDENTITY(1,1) NOT NULL,
	[IdTemporada] [int] NOT NULL,
	[IdEquipo] [int] NOT NULL,
	[NombreIglesia] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[PastorPrincipal] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Ubicacion] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CajitasEntregadas] [int] NOT NULL,
	[InscritosLGA] [int] NOT NULL,
	[FechaPlantacion] [date] NULL,
	[Notas] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FechaCreacion] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdIglesiaPlantada] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[EOS_IglesiasPlantadas] ADD  DEFAULT ((0)) FOR [CajitasEntregadas]
ALTER TABLE [dbo].[EOS_IglesiasPlantadas] ADD  DEFAULT ((0)) FOR [InscritosLGA]
ALTER TABLE [dbo].[EOS_IglesiasPlantadas] ADD  DEFAULT (getdate()) FOR [FechaCreacion]
ALTER TABLE [dbo].[EOS_IglesiasPlantadas]  WITH CHECK ADD FOREIGN KEY([IdEquipo])
REFERENCES [dbo].[Equipos] ([IdEquipo])
ALTER TABLE [dbo].[EOS_IglesiasPlantadas]  WITH CHECK ADD FOREIGN KEY([IdTemporada])
REFERENCES [dbo].[Temporadas] ([IdTemporada])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[EOS_MentoreoViajes](
	[IdViajeMentoreo] [int] IDENTITY(1,1) NOT NULL,
	[IdTemporada] [int] NOT NULL,
	[IdEquipo] [int] NOT NULL,
	[FechaViaje] [date] NOT NULL,
	[Destino] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Objetivo] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CoordinadorResponsable] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[MontoGastadoDOP] [decimal](18, 2) NOT NULL,
	[MontoGastadoUSD] [decimal](18, 2) NOT NULL,
	[FechaCreacion] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdViajeMentoreo] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[EOS_MentoreoViajes] ADD  DEFAULT ((0)) FOR [MontoGastadoDOP]
ALTER TABLE [dbo].[EOS_MentoreoViajes] ADD  DEFAULT ((0)) FOR [MontoGastadoUSD]
ALTER TABLE [dbo].[EOS_MentoreoViajes] ADD  DEFAULT (getdate()) FOR [FechaCreacion]
ALTER TABLE [dbo].[EOS_MentoreoViajes]  WITH CHECK ADD FOREIGN KEY([IdEquipo])
REFERENCES [dbo].[Equipos] ([IdEquipo])
ALTER TABLE [dbo].[EOS_MentoreoViajes]  WITH CHECK ADD FOREIGN KEY([IdTemporada])
REFERENCES [dbo].[Temporadas] ([IdTemporada])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Equipos](
	[IdEquipo] [int] IDENTITY(1,1) NOT NULL,
	[NombreEquipo] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[IdNivelEquipo] [int] NOT NULL,
	[IdEquipoPadre] [int] NULL,
	[Activo] [bit] NOT NULL,
	[RowVersion] [timestamp] NOT NULL,
	[FechaModificacion] [datetime2](7) NULL,
	[UsuarioModificacion] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdEquipo] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
SET IDENTITY_INSERT [dbo].[Equipos] ON 

INSERT [dbo].[Equipos] ([IdEquipo], [NombreEquipo], [IdNivelEquipo], [IdEquipoPadre], [Activo], [FechaModificacion], [UsuarioModificacion]) VALUES (1, N'Equipo Nacional de Liderazgo', 1, NULL, 1, NULL, NULL)
INSERT [dbo].[Equipos] ([IdEquipo], [NombreEquipo], [IdNivelEquipo], [IdEquipoPadre], [Activo], [FechaModificacion], [UsuarioModificacion]) VALUES (2, N'ERLE Santo Domingo', 2, 1, 1, NULL, NULL)
INSERT [dbo].[Equipos] ([IdEquipo], [NombreEquipo], [IdNivelEquipo], [IdEquipoPadre], [Activo], [FechaModificacion], [UsuarioModificacion]) VALUES (23, N'ERL Santo Domingo Oeste', 3, 24, 1, NULL, NULL)
INSERT [dbo].[Equipos] ([IdEquipo], [NombreEquipo], [IdNivelEquipo], [IdEquipoPadre], [Activo], [FechaModificacion], [UsuarioModificacion]) VALUES (24, N'ÉRLE Nuevo', 2, 1, 1, NULL, NULL)
SET IDENTITY_INSERT [dbo].[Equipos] OFF
ALTER TABLE [dbo].[Equipos] ADD  DEFAULT ((1)) FOR [Activo]
ALTER TABLE [dbo].[Equipos]  WITH CHECK ADD FOREIGN KEY([IdEquipoPadre])
REFERENCES [dbo].[Equipos] ([IdEquipo])
ALTER TABLE [dbo].[Equipos]  WITH CHECK ADD FOREIGN KEY([IdNivelEquipo])
REFERENCES [dbo].[NivelesEquipo] ([IdNivelEquipo])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[ERLE_Categorias](
	[CategoriaId] [varchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Tipo] [varchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Grupo] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Descripcion] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Orden] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[CategoriaId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
INSERT [dbo].[ERLE_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'E-0', N'GASTO', N'ENTRENAMIENTO', N'Envío, Retiro o Transferencia para Entrenamientos', 5)
INSERT [dbo].[ERLE_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'E-1', N'GASTO', N'ENTRENAMIENTO', N'Transporte', 6)
INSERT [dbo].[ERLE_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'E-2', N'GASTO', N'ENTRENAMIENTO', N'Snacks o Refrigerios', 7)
INSERT [dbo].[ERLE_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'E-3', N'GASTO', N'ENTRENAMIENTO', N'Alimento', 8)
INSERT [dbo].[ERLE_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'E-4', N'GASTO', N'ENTRENAMIENTO', N'Administración y Otros Gastos de Oficina', 9)
INSERT [dbo].[ERLE_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'I-1', N'INGRESO', N'INGRESOS', N'Subvención - Entrenamientos', 1)
INSERT [dbo].[ERLE_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'I-2', N'INGRESO', N'INGRESOS', N'Subvención - Mentoreo', 2)
INSERT [dbo].[ERLE_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'I-3', N'INGRESO', N'INGRESOS', N'Ingresos para Logística', 3)
INSERT [dbo].[ERLE_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'I-4', N'INGRESO', N'INGRESOS', N'Otros Ingresos', 4)
INSERT [dbo].[ERLE_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'L-1', N'GASTO', N'LOGISTICA', N'Transporte de Cajitas y Literatura', 15)
INSERT [dbo].[ERLE_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'L-2', N'GASTO', N'LOGISTICA', N'Almacenaje de Cajitas y Literatura', 16)
INSERT [dbo].[ERLE_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'L-3', N'GASTO', N'LOGISTICA', N'Otros Gastos de Logística', 17)
INSERT [dbo].[ERLE_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'M-0', N'GASTO', N'MENTOREO', N'Envío, Retiro o Transferencia para Mentoreo', 10)
INSERT [dbo].[ERLE_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'M-1', N'GASTO', N'MENTOREO', N'Transporte', 11)
INSERT [dbo].[ERLE_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'M-2', N'GASTO', N'MENTOREO', N'Alimento', 12)
INSERT [dbo].[ERLE_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'M-3', N'GASTO', N'MENTOREO', N'Hospedaje', 13)
INSERT [dbo].[ERLE_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'M-4', N'GASTO', N'MENTOREO', N'Administración y Otros Gastos de Oficina', 14)
INSERT [dbo].[ERLE_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'O-1', N'GASTO', N'OTROS', N'Otros eventos o gastos aprobados', 18)
ALTER TABLE [dbo].[ERLE_Categorias] ADD  DEFAULT ((0)) FOR [Orden]
ALTER TABLE [dbo].[ERLE_Categorias]  WITH CHECK ADD CHECK  (([Tipo]='GASTO' OR [Tipo]='INGRESO'))

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[ERLE_Equipos](
	[EquipoId] [int] IDENTITY(1,1) NOT NULL,
	[Codigo] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Nombre] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[EsENL] [bit] NOT NULL,
	[Activo] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[EquipoId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
SET IDENTITY_INSERT [dbo].[ERLE_Equipos] ON 

INSERT [dbo].[ERLE_Equipos] ([EquipoId], [Codigo], [Nombre], [EsENL], [Activo]) VALUES (1, N'DN-ERLE', N'Distrito Nacional - ERLE', 1, 1)
INSERT [dbo].[ERLE_Equipos] ([EquipoId], [Codigo], [Nombre], [EsENL], [Activo]) VALUES (2, N'EQ-1', N'Equipo Nacional de Liderazgo', 1, 1)
INSERT [dbo].[ERLE_Equipos] ([EquipoId], [Codigo], [Nombre], [EsENL], [Activo]) VALUES (3, N'EQ-2', N'ERLE Santo Domingo', 0, 1)
INSERT [dbo].[ERLE_Equipos] ([EquipoId], [Codigo], [Nombre], [EsENL], [Activo]) VALUES (4, N'EQ-23', N'ERL Santo Domingo Oeste', 0, 1)
INSERT [dbo].[ERLE_Equipos] ([EquipoId], [Codigo], [Nombre], [EsENL], [Activo]) VALUES (5, N'EQ-24', N'ÉRLE Nuevo', 0, 1)
SET IDENTITY_INSERT [dbo].[ERLE_Equipos] OFF
SET ANSI_PADDING ON

ALTER TABLE [dbo].[ERLE_Equipos] ADD UNIQUE NONCLUSTERED 
(
	[Codigo] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[ERLE_Equipos] ADD  DEFAULT ((0)) FOR [EsENL]
ALTER TABLE [dbo].[ERLE_Equipos] ADD  DEFAULT ((1)) FOR [Activo]

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[ERLE_PresupuestosAprobados](
	[PresupuestoId] [int] IDENTITY(1,1) NOT NULL,
	[TemporadaId] [int] NOT NULL,
	[EquipoId] [int] NOT NULL,
	[CategoriaId] [varchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[MontoAprobadoUSD] [decimal](18, 2) NOT NULL,
	[MontoAprobadoDOP]  AS (CONVERT([decimal](18,2),round([MontoAprobadoUSD]*(58.63),(2)))),
PRIMARY KEY CLUSTERED 
(
	[PresupuestoId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
SET IDENTITY_INSERT [dbo].[ERLE_PresupuestosAprobados] ON 

INSERT [dbo].[ERLE_PresupuestosAprobados] ([PresupuestoId], [TemporadaId], [EquipoId], [CategoriaId], [MontoAprobadoUSD]) VALUES (1, 1, 1, N'I-1', CAST(3685.00 AS Decimal(18, 2)))
INSERT [dbo].[ERLE_PresupuestosAprobados] ([PresupuestoId], [TemporadaId], [EquipoId], [CategoriaId], [MontoAprobadoUSD]) VALUES (2, 1, 1, N'I-2', CAST(1200.00 AS Decimal(18, 2)))
INSERT [dbo].[ERLE_PresupuestosAprobados] ([PresupuestoId], [TemporadaId], [EquipoId], [CategoriaId], [MontoAprobadoUSD]) VALUES (3, 1, 1, N'E-1', CAST(546.00 AS Decimal(18, 2)))
INSERT [dbo].[ERLE_PresupuestosAprobados] ([PresupuestoId], [TemporadaId], [EquipoId], [CategoriaId], [MontoAprobadoUSD]) VALUES (4, 1, 1, N'E-2', CAST(298.00 AS Decimal(18, 2)))
INSERT [dbo].[ERLE_PresupuestosAprobados] ([PresupuestoId], [TemporadaId], [EquipoId], [CategoriaId], [MontoAprobadoUSD]) VALUES (5, 1, 1, N'E-3', CAST(2148.00 AS Decimal(18, 2)))
INSERT [dbo].[ERLE_PresupuestosAprobados] ([PresupuestoId], [TemporadaId], [EquipoId], [CategoriaId], [MontoAprobadoUSD]) VALUES (6, 1, 1, N'E-4', CAST(693.00 AS Decimal(18, 2)))
INSERT [dbo].[ERLE_PresupuestosAprobados] ([PresupuestoId], [TemporadaId], [EquipoId], [CategoriaId], [MontoAprobadoUSD]) VALUES (7, 1, 1, N'M-1', CAST(140.00 AS Decimal(18, 2)))
INSERT [dbo].[ERLE_PresupuestosAprobados] ([PresupuestoId], [TemporadaId], [EquipoId], [CategoriaId], [MontoAprobadoUSD]) VALUES (8, 1, 1, N'M-2', CAST(160.00 AS Decimal(18, 2)))
INSERT [dbo].[ERLE_PresupuestosAprobados] ([PresupuestoId], [TemporadaId], [EquipoId], [CategoriaId], [MontoAprobadoUSD]) VALUES (9, 1, 1, N'M-3', CAST(150.00 AS Decimal(18, 2)))
INSERT [dbo].[ERLE_PresupuestosAprobados] ([PresupuestoId], [TemporadaId], [EquipoId], [CategoriaId], [MontoAprobadoUSD]) VALUES (10, 1, 1, N'M-4', CAST(750.00 AS Decimal(18, 2)))
SET IDENTITY_INSERT [dbo].[ERLE_PresupuestosAprobados] OFF
SET ANSI_PADDING ON

ALTER TABLE [dbo].[ERLE_PresupuestosAprobados] ADD  CONSTRAINT [UQ_ERLE_Presupuesto] UNIQUE NONCLUSTERED 
(
	[TemporadaId] ASC,
	[EquipoId] ASC,
	[CategoriaId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[ERLE_PresupuestosAprobados] ADD  DEFAULT ((0)) FOR [MontoAprobadoUSD]
ALTER TABLE [dbo].[ERLE_PresupuestosAprobados]  WITH CHECK ADD FOREIGN KEY([CategoriaId])
REFERENCES [dbo].[ERLE_Categorias] ([CategoriaId])
ALTER TABLE [dbo].[ERLE_PresupuestosAprobados]  WITH CHECK ADD FOREIGN KEY([EquipoId])
REFERENCES [dbo].[ERLE_Equipos] ([EquipoId])
ALTER TABLE [dbo].[ERLE_PresupuestosAprobados]  WITH CHECK ADD FOREIGN KEY([TemporadaId])
REFERENCES [dbo].[ERLE_Temporadas] ([TemporadaId])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[ERLE_Temporadas](
	[TemporadaId] [int] IDENTITY(1,1) NOT NULL,
	[Nombre] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[TasaCambioReferencia] [decimal](10, 4) NOT NULL,
	[Activa] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[TemporadaId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
SET IDENTITY_INSERT [dbo].[ERLE_Temporadas] ON 

INSERT [dbo].[ERLE_Temporadas] ([TemporadaId], [Nombre], [TasaCambioReferencia], [Activa]) VALUES (1, N'2026 - 2027', CAST(58.6300 AS Decimal(10, 4)), 1)
INSERT [dbo].[ERLE_Temporadas] ([TemporadaId], [Nombre], [TasaCambioReferencia], [Activa]) VALUES (2, N'Temp 2026-2027', CAST(58.6300 AS Decimal(10, 4)), 1)
INSERT [dbo].[ERLE_Temporadas] ([TemporadaId], [Nombre], [TasaCambioReferencia], [Activa]) VALUES (3, N'Temp 2025-2026', CAST(58.6300 AS Decimal(10, 4)), 0)
INSERT [dbo].[ERLE_Temporadas] ([TemporadaId], [Nombre], [TasaCambioReferencia], [Activa]) VALUES (4, N'Temp 2024-2025', CAST(58.6300 AS Decimal(10, 4)), 0)
INSERT [dbo].[ERLE_Temporadas] ([TemporadaId], [Nombre], [TasaCambioReferencia], [Activa]) VALUES (5, N'Temp 2023-2024', CAST(58.6300 AS Decimal(10, 4)), 0)
SET IDENTITY_INSERT [dbo].[ERLE_Temporadas] OFF
SET ANSI_PADDING ON

ALTER TABLE [dbo].[ERLE_Temporadas] ADD UNIQUE NONCLUSTERED 
(
	[Nombre] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[ERLE_Temporadas] ADD  DEFAULT ((58.63)) FOR [TasaCambioReferencia]
ALTER TABLE [dbo].[ERLE_Temporadas] ADD  DEFAULT ((1)) FOR [Activa]

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[ERLE_Transacciones](
	[TransaccionId] [bigint] IDENTITY(1,1) NOT NULL,
	[TemporadaId] [int] NOT NULL,
	[EquipoId] [int] NOT NULL,
	[Mes] [varchar](3) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Fecha] [date] NOT NULL,
	[NumeroDocumento] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Descripcion] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CategoriaId] [varchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[GastoDOP] [decimal](18, 2) NOT NULL,
	[IngresoDOP] [decimal](18, 2) NOT NULL,
	[TasaCambio] [decimal](10, 4) NOT NULL,
	[GastoUSD]  AS (CONVERT([decimal](18,2),round([GastoDOP]/nullif([TasaCambio],(0)),(2)))),
	[IngresoUSD]  AS (CONVERT([decimal](18,2),round([IngresoDOP]/nullif([TasaCambio],(0)),(2)))),
	[Notas] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FechaCreacion] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[TransaccionId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[ERLE_Transacciones] ADD  DEFAULT ((0)) FOR [GastoDOP]
ALTER TABLE [dbo].[ERLE_Transacciones] ADD  DEFAULT ((0)) FOR [IngresoDOP]
ALTER TABLE [dbo].[ERLE_Transacciones] ADD  DEFAULT ((58.63)) FOR [TasaCambio]
ALTER TABLE [dbo].[ERLE_Transacciones] ADD  DEFAULT (getdate()) FOR [FechaCreacion]
ALTER TABLE [dbo].[ERLE_Transacciones]  WITH CHECK ADD FOREIGN KEY([CategoriaId])
REFERENCES [dbo].[ERLE_Categorias] ([CategoriaId])
ALTER TABLE [dbo].[ERLE_Transacciones]  WITH CHECK ADD FOREIGN KEY([EquipoId])
REFERENCES [dbo].[ERLE_Equipos] ([EquipoId])
ALTER TABLE [dbo].[ERLE_Transacciones]  WITH CHECK ADD FOREIGN KEY([TemporadaId])
REFERENCES [dbo].[ERLE_Temporadas] ([TemporadaId])
ALTER TABLE [dbo].[ERLE_Transacciones]  WITH CHECK ADD CHECK  (([Mes]='AGO' OR [Mes]='JUL' OR [Mes]='JUN' OR [Mes]='MAY' OR [Mes]='ABR' OR [Mes]='MAR' OR [Mes]='FEB' OR [Mes]='ENE' OR [Mes]='DIC' OR [Mes]='NOV' OR [Mes]='OCT' OR [Mes]='SEP'))

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[EstadosCuenta](
	[IdEstado] [int] NOT NULL,
	[NombreEstado] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Descripcion] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[IdEstado] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
INSERT [dbo].[EstadosCuenta] ([IdEstado], [NombreEstado], [Descripcion]) VALUES (1, N'PendienteAprobacionCorreo', N'Usuario recién registrado, correo pendiente de aprobación por admin')
INSERT [dbo].[EstadosCuenta] ([IdEstado], [NombreEstado], [Descripcion]) VALUES (2, N'CorreoAprobado', N'Correo aprobado por admin, pendiente de llenar formulario de coordinador')
INSERT [dbo].[EstadosCuenta] ([IdEstado], [NombreEstado], [Descripcion]) VALUES (3, N'PerfilPendienteAprobacion', N'Formulario completado, pendiente de aprobación final por admin')
INSERT [dbo].[EstadosCuenta] ([IdEstado], [NombreEstado], [Descripcion]) VALUES (4, N'Activo', N'Usuario plenamente activo con acceso al sistema')
INSERT [dbo].[EstadosCuenta] ([IdEstado], [NombreEstado], [Descripcion]) VALUES (5, N'Rechazado', N'Solicitud rechazada')
INSERT [dbo].[EstadosCuenta] ([IdEstado], [NombreEstado], [Descripcion]) VALUES (6, N'Suspendido', N'Usuario suspendido')
INSERT [dbo].[EstadosCuenta] ([IdEstado], [NombreEstado], [Descripcion]) VALUES (7, N'Pendiente Restablecimiento', NULL)
INSERT [dbo].[EstadosCuenta] ([IdEstado], [NombreEstado], [Descripcion]) VALUES (8, N'Aprobado Restablecimiento', NULL)

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Eventos](
	[IdEvento] [int] IDENTITY(1,1) NOT NULL,
	[NombreEvento] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[TipoEvento] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[IdTemporada] [int] NOT NULL,
	[Fecha] [date] NOT NULL,
	[Lugar] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Responsable] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IdUsuarioCreacion] [int] NOT NULL,
	[FechaCreacion] [datetime] NULL,
	[TipoLugar] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Hora] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CantidadAsistentes] [int] NULL,
	[RowVersion] [timestamp] NOT NULL,
	[FechaModificacion] [datetime2](7) NULL,
	[UsuarioModificacion] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdEvento] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
SET IDENTITY_INSERT [dbo].[Eventos] ON 

INSERT [dbo].[Eventos] ([IdEvento], [NombreEvento], [TipoEvento], [IdTemporada], [Fecha], [Lugar], [Responsable], [IdUsuarioCreacion], [FechaCreacion], [TipoLugar], [Hora], [CantidadAsistentes], [FechaModificacion], [UsuarioModificacion]) VALUES (1, N'PV 1', N'Vision', 1, CAST(N'2026-08-24' AS Date), N'Templo Solo', N'CMI Rol', 2, CAST(N'2026-08-24T23:21:09.000' AS DateTime), N'Templo', N'09:00', 2, NULL, NULL)
INSERT [dbo].[Eventos] ([IdEvento], [NombreEvento], [TipoEvento], [IdTemporada], [Fecha], [Lugar], [Responsable], [IdUsuarioCreacion], [FechaCreacion], [TipoLugar], [Hora], [CantidadAsistentes], [FechaModificacion], [UsuarioModificacion]) VALUES (2, N'Taller 1', N'Taller', 1, CAST(N'2026-08-25' AS Date), N'Templo Solo', N'CD Rol', 3, CAST(N'2026-08-24T23:37:26.953' AS DateTime), N'Templo', N'09:01', 2, NULL, NULL)
INSERT [dbo].[Eventos] ([IdEvento], [NombreEvento], [TipoEvento], [IdTemporada], [Fecha], [Lugar], [Responsable], [IdUsuarioCreacion], [FechaCreacion], [TipoLugar], [Hora], [CantidadAsistentes], [FechaModificacion], [UsuarioModificacion]) VALUES (3, N'Jornada Despacho Automatizada', N'Despacho', 1, CAST(N'2026-09-02' AS Date), N'Lugar Test', N'Coordinador Responsable', 1, CAST(N'2026-09-02T02:54:51.020' AS DateTime), NULL, N'09:00 AM', 0, NULL, NULL)
INSERT [dbo].[Eventos] ([IdEvento], [NombreEvento], [TipoEvento], [IdTemporada], [Fecha], [Lugar], [Responsable], [IdUsuarioCreacion], [FechaCreacion], [TipoLugar], [Hora], [CantidadAsistentes], [FechaModificacion], [UsuarioModificacion]) VALUES (4, N'Jornada Despacho Automatizada', N'Despacho', 1, CAST(N'2026-09-02' AS Date), N'Lugar Test', N'Coordinador Responsable', 1, CAST(N'2026-09-02T02:55:12.230' AS DateTime), NULL, N'09:00 AM', 0, NULL, NULL)
INSERT [dbo].[Eventos] ([IdEvento], [NombreEvento], [TipoEvento], [IdTemporada], [Fecha], [Lugar], [Responsable], [IdUsuarioCreacion], [FechaCreacion], [TipoLugar], [Hora], [CantidadAsistentes], [FechaModificacion], [UsuarioModificacion]) VALUES (5, N'Evento Despacho Test Oficial', N'Despacho', 1, CAST(N'2026-09-02' AS Date), N'Sede Central Test', NULL, 1, CAST(N'2026-09-02T03:30:13.287' AS DateTime), NULL, NULL, 0, NULL, NULL)
INSERT [dbo].[Eventos] ([IdEvento], [NombreEvento], [TipoEvento], [IdTemporada], [Fecha], [Lugar], [Responsable], [IdUsuarioCreacion], [FechaCreacion], [TipoLugar], [Hora], [CantidadAsistentes], [FechaModificacion], [UsuarioModificacion]) VALUES (6, N'Despacho Unificado Test 04661c5d', N'Despacho', 1, CAST(N'2026-09-02' AS Date), N'Centro Test', N'Coordinador', 1, CAST(N'2026-09-02T03:41:55.613' AS DateTime), N'Salon', N'10:00', 40, NULL, NULL)
INSERT [dbo].[Eventos] ([IdEvento], [NombreEvento], [TipoEvento], [IdTemporada], [Fecha], [Lugar], [Responsable], [IdUsuarioCreacion], [FechaCreacion], [TipoLugar], [Hora], [CantidadAsistentes], [FechaModificacion], [UsuarioModificacion]) VALUES (7, N'PV #2', N'Vision', 1, CAST(N'2026-09-02' AS Date), N'Templo Sillon', N'Efesos Astacio', 1, CAST(N'2026-09-02T16:58:03.147' AS DateTime), N'Templo', N'09:00', 2, NULL, NULL)
INSERT [dbo].[Eventos] ([IdEvento], [NombreEvento], [TipoEvento], [IdTemporada], [Fecha], [Lugar], [Responsable], [IdUsuarioCreacion], [FechaCreacion], [TipoLugar], [Hora], [CantidadAsistentes], [FechaModificacion], [UsuarioModificacion]) VALUES (8, N'Taller OCC #2', N'Taller', 1, CAST(N'2026-09-02' AS Date), N'Templo Sillon', N'Efesos Astacio', 1, CAST(N'2026-09-02T17:02:00.383' AS DateTime), N'Templo', N'09:00', 2, NULL, NULL)
INSERT [dbo].[Eventos] ([IdEvento], [NombreEvento], [TipoEvento], [IdTemporada], [Fecha], [Lugar], [Responsable], [IdUsuarioCreacion], [FechaCreacion], [TipoLugar], [Hora], [CantidadAsistentes], [FechaModificacion], [UsuarioModificacion]) VALUES (9, N'Despacho #2', N'Despacho', 1, CAST(N'2026-09-02' AS Date), N'FUNJEMAR', N'Efesos Astacio', 1, CAST(N'2026-09-02T18:02:47.903' AS DateTime), N'Almacén', N'09:00', 0, NULL, NULL)
SET IDENTITY_INSERT [dbo].[Eventos] OFF
CREATE NONCLUSTERED INDEX [IX_Eventos_IdTemporada_Fecha] ON [dbo].[Eventos]
(
	[IdTemporada] ASC,
	[Fecha] DESC
)
INCLUDE([NombreEvento],[TipoEvento],[Lugar],[Responsable],[CantidadAsistentes]) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[Eventos] ADD  DEFAULT (getdate()) FOR [FechaCreacion]
ALTER TABLE [dbo].[Eventos] ADD  DEFAULT ((0)) FOR [CantidadAsistentes]
ALTER TABLE [dbo].[Eventos]  WITH CHECK ADD FOREIGN KEY([IdTemporada])
REFERENCES [dbo].[Temporadas] ([IdTemporada])
ALTER TABLE [dbo].[Eventos]  WITH CHECK ADD FOREIGN KEY([IdUsuarioCreacion])
REFERENCES [dbo].[Usuarios] ([IdUsuario])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[EventosAsistenciaCoordinadores](
	[IdAsistenciaCoordinador] [int] IDENTITY(1,1) NOT NULL,
	[IdEvento] [int] NOT NULL,
	[IdUsuario] [int] NOT NULL,
	[Asistio] [bit] NOT NULL,
	[RolEnEvento] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Observaciones] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FechaRegistro] [datetime] NOT NULL,
	[IdUsuarioRegistro] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdAsistenciaCoordinador] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

CREATE NONCLUSTERED INDEX [IX_EventosAsistenciaCoordinadores_Evento] ON [dbo].[EventosAsistenciaCoordinadores]
(
	[IdEvento] ASC,
	[Asistio] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
CREATE UNIQUE NONCLUSTERED INDEX [IX_EventosAsistenciaCoordinadores_Evento_Usuario] ON [dbo].[EventosAsistenciaCoordinadores]
(
	[IdEvento] ASC,
	[IdUsuario] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[EventosAsistenciaCoordinadores] ADD  DEFAULT ((1)) FOR [Asistio]
ALTER TABLE [dbo].[EventosAsistenciaCoordinadores] ADD  DEFAULT (getdate()) FOR [FechaRegistro]
ALTER TABLE [dbo].[EventosAsistenciaCoordinadores]  WITH CHECK ADD FOREIGN KEY([IdEvento])
REFERENCES [dbo].[Eventos] ([IdEvento])
ALTER TABLE [dbo].[EventosAsistenciaCoordinadores]  WITH CHECK ADD FOREIGN KEY([IdUsuario])
REFERENCES [dbo].[Usuarios] ([IdUsuario])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[EventosAsistentes](
	[IdAsistente] [int] IDENTITY(1,1) NOT NULL,
	[IdEvento] [int] NOT NULL,
	[IdParticipacion] [int] NOT NULL,
	[NombreCompleto] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Identificacion] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Telefono] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Correo] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FechaRegistro] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdAsistente] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET IDENTITY_INSERT [dbo].[EventosAsistentes] ON 

INSERT [dbo].[EventosAsistentes] ([IdAsistente], [IdEvento], [IdParticipacion], [NombreCompleto], [Identificacion], [Telefono], [Correo], [FechaRegistro]) VALUES (26, 1, 2, N'Pastor Apellido', N'40224472406', N'8295656565', N'erlegsd.occrd@gmail.com', CAST(N'2026-08-24T23:25:19.963' AS DateTime))
INSERT [dbo].[EventosAsistentes] ([IdAsistente], [IdEvento], [IdParticipacion], [NombreCompleto], [Identificacion], [Telefono], [Correo], [FechaRegistro]) VALUES (27, 1, 2, N'Lider Apellido', N'40258789654', N'8497898523', N'erlegsd.occrd@gmail.com', CAST(N'2026-08-24T23:25:19.963' AS DateTime))
INSERT [dbo].[EventosAsistentes] ([IdAsistente], [IdEvento], [IdParticipacion], [NombreCompleto], [Identificacion], [Telefono], [Correo], [FechaRegistro]) VALUES (30, 2, 2, N'Lider Apellido', N'40258789654', N'8497898523', N'erlegsd.occrd@gmail.com', CAST(N'2026-08-24T23:54:37.010' AS DateTime))
INSERT [dbo].[EventosAsistentes] ([IdAsistente], [IdEvento], [IdParticipacion], [NombreCompleto], [Identificacion], [Telefono], [Correo], [FechaRegistro]) VALUES (31, 2, 2, N'Maria LA Del Barrio', N'40225455856', N'8092323518', N'erlegsd.occrd@gmail.com', CAST(N'2026-08-24T23:54:37.010' AS DateTime))
INSERT [dbo].[EventosAsistentes] ([IdAsistente], [IdEvento], [IdParticipacion], [NombreCompleto], [Identificacion], [Telefono], [Correo], [FechaRegistro]) VALUES (33, 7, 3, N'Servicio Al Cliente', N'40224442406', N'8295656565', N'portaforza@gmail.com', CAST(N'2026-09-02T16:58:52.503' AS DateTime))
INSERT [dbo].[EventosAsistentes] ([IdAsistente], [IdEvento], [IdParticipacion], [NombreCompleto], [Identificacion], [Telefono], [Correo], [FechaRegistro]) VALUES (34, 7, 3, N'Servicio Al Cliente', N'40145856587', N'8497898523', N'portaforza@gmail.com', CAST(N'2026-09-02T16:58:52.503' AS DateTime))
INSERT [dbo].[EventosAsistentes] ([IdAsistente], [IdEvento], [IdParticipacion], [NombreCompleto], [Identificacion], [Telefono], [Correo], [FechaRegistro]) VALUES (35, 8, 3, N'Servicio Al Cliente', N'40145856587', N'8497898523', N'portaforza@gmail.com', CAST(N'2026-09-02T17:11:31.950' AS DateTime))
INSERT [dbo].[EventosAsistentes] ([IdAsistente], [IdEvento], [IdParticipacion], [NombreCompleto], [Identificacion], [Telefono], [Correo], [FechaRegistro]) VALUES (36, 8, 3, N'Sergio Torres', N'40256998524', N'8095624578', N'portaforza@gmail.com', CAST(N'2026-09-02T17:11:31.953' AS DateTime))
SET IDENTITY_INSERT [dbo].[EventosAsistentes] OFF
CREATE NONCLUSTERED INDEX [IX_EventosAsistentes_IdEvento] ON [dbo].[EventosAsistentes]
(
	[IdEvento] ASC,
	[IdParticipacion] ASC
)
INCLUDE([NombreCompleto],[Identificacion],[Telefono],[Correo]) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
SET ANSI_PADDING ON

CREATE UNIQUE NONCLUSTERED INDEX [UQ_EventosAsistentes_Evento_Participacion_Doc] ON [dbo].[EventosAsistentes]
(
	[IdEvento] ASC,
	[IdParticipacion] ASC,
	[Identificacion] ASC
)
WHERE ([Identificacion] IS NOT NULL AND [Identificacion]<>'')
WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[EventosAsistentes] ADD  DEFAULT (getdate()) FOR [FechaRegistro]

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[EventosDespacho](
	[IdEventoDespacho] [int] IDENTITY(1,1) NOT NULL,
	[IdEvento] [int] NOT NULL,
	[IdAlmacen] [int] NULL,
	[IdEquipo] [int] NOT NULL,
	[EstadoDespachoEvento] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdEventoDespacho] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
SET IDENTITY_INSERT [dbo].[EventosDespacho] ON 

INSERT [dbo].[EventosDespacho] ([IdEventoDespacho], [IdEvento], [IdAlmacen], [IdEquipo], [EstadoDespachoEvento]) VALUES (1, 3, 1, 1, N'PROGRAMADO')
INSERT [dbo].[EventosDespacho] ([IdEventoDespacho], [IdEvento], [IdAlmacen], [IdEquipo], [EstadoDespachoEvento]) VALUES (2, 4, 1, 1, N'PROGRAMADO')
INSERT [dbo].[EventosDespacho] ([IdEventoDespacho], [IdEvento], [IdAlmacen], [IdEquipo], [EstadoDespachoEvento]) VALUES (3, 5, NULL, 1, N'PROGRAMADO')
INSERT [dbo].[EventosDespacho] ([IdEventoDespacho], [IdEvento], [IdAlmacen], [IdEquipo], [EstadoDespachoEvento]) VALUES (4, 6, NULL, 1, N'PROGRAMADO')
INSERT [dbo].[EventosDespacho] ([IdEventoDespacho], [IdEvento], [IdAlmacen], [IdEquipo], [EstadoDespachoEvento]) VALUES (5, 9, 2, 24, N'PROGRAMADO')
SET IDENTITY_INSERT [dbo].[EventosDespacho] OFF
ALTER TABLE [dbo].[EventosDespacho] ADD UNIQUE NONCLUSTERED 
(
	[IdEvento] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[EventosDespacho] ADD  DEFAULT ('PROGRAMADO') FOR [EstadoDespachoEvento]
ALTER TABLE [dbo].[EventosDespacho]  WITH CHECK ADD FOREIGN KEY([IdAlmacen])
REFERENCES [dbo].[Almacenes] ([IdAlmacen])
ALTER TABLE [dbo].[EventosDespacho]  WITH CHECK ADD FOREIGN KEY([IdEquipo])
REFERENCES [dbo].[Equipos] ([IdEquipo])
ALTER TABLE [dbo].[EventosDespacho]  WITH CHECK ADD FOREIGN KEY([IdEvento])
REFERENCES [dbo].[Eventos] ([IdEvento])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[EventosParticipacionIglesia](
	[IdEventoParticipacion] [int] IDENTITY(1,1) NOT NULL,
	[IdEvento] [int] NOT NULL,
	[IdParticipacion] [int] NOT NULL,
	[Asistio] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdEventoParticipacion] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET IDENTITY_INSERT [dbo].[EventosParticipacionIglesia] ON 

INSERT [dbo].[EventosParticipacionIglesia] ([IdEventoParticipacion], [IdEvento], [IdParticipacion], [Asistio]) VALUES (11, 1, 2, 1)
INSERT [dbo].[EventosParticipacionIglesia] ([IdEventoParticipacion], [IdEvento], [IdParticipacion], [Asistio]) VALUES (12, 2, 2, 1)
INSERT [dbo].[EventosParticipacionIglesia] ([IdEventoParticipacion], [IdEvento], [IdParticipacion], [Asistio]) VALUES (13, 7, 3, 1)
INSERT [dbo].[EventosParticipacionIglesia] ([IdEventoParticipacion], [IdEvento], [IdParticipacion], [Asistio]) VALUES (14, 8, 3, 1)
SET IDENTITY_INSERT [dbo].[EventosParticipacionIglesia] OFF
ALTER TABLE [dbo].[EventosParticipacionIglesia] ADD  DEFAULT ((1)) FOR [Asistio]
ALTER TABLE [dbo].[EventosParticipacionIglesia]  WITH CHECK ADD FOREIGN KEY([IdEvento])
REFERENCES [dbo].[Eventos] ([IdEvento])
ALTER TABLE [dbo].[EventosParticipacionIglesia]  WITH CHECK ADD FOREIGN KEY([IdParticipacion])
REFERENCES [dbo].[ParticipacionesIglesia] ([IdParticipacion])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[EvidenciasRecepcion](
	[IdEvidencia] [int] IDENTITY(1,1) NOT NULL,
	[IdRecepcion] [int] NOT NULL,
	[NombreArchivo] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[RutaArchivo] [varchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[TipoContenido] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TamanoBytes] [bigint] NULL,
	[FechaCarga] [datetime2](7) NOT NULL,
	[IdUsuario] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdEvidencia] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[EvidenciasRecepcion] ADD  DEFAULT (getdate()) FOR [FechaCarga]
ALTER TABLE [dbo].[EvidenciasRecepcion]  WITH CHECK ADD FOREIGN KEY([IdRecepcion])
REFERENCES [dbo].[RecepcionesContenedor] ([IdRecepcion])
ALTER TABLE [dbo].[EvidenciasRecepcion]  WITH CHECK ADD FOREIGN KEY([IdUsuario])
REFERENCES [dbo].[Usuarios] ([IdUsuario])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[EvidenciasRecepcionContenedor](
	[IdEvidencia] [int] IDENTITY(1,1) NOT NULL,
	[IdRecepcion] [int] NOT NULL,
	[NombreArchivo] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[RutaArchivo] [varchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[TipoContenido] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TamanoBytes] [bigint] NULL,
	[IdUsuarioRegistro] [int] NOT NULL,
	[FechaRegistro] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdEvidencia] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[EvidenciasRecepcionContenedor] ADD  DEFAULT (getdate()) FOR [FechaRegistro]
ALTER TABLE [dbo].[EvidenciasRecepcionContenedor]  WITH CHECK ADD FOREIGN KEY([IdRecepcion])
REFERENCES [dbo].[RecepcionesContenedor] ([IdRecepcion])
ON DELETE CASCADE
ALTER TABLE [dbo].[EvidenciasRecepcionContenedor]  WITH CHECK ADD FOREIGN KEY([IdUsuarioRegistro])
REFERENCES [dbo].[Usuarios] ([IdUsuario])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[ExcepcionesRegla3Anios](
	[IdExcepcion] [int] IDENTITY(1,1) NOT NULL,
	[IdIglesia] [int] NOT NULL,
	[IdTemporada] [int] NOT NULL,
	[TemporadaPreviaId] [int] NULL,
	[DiferenciaTemporadas] [int] NOT NULL,
	[Motivo] [nvarchar](250) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Justificacion] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ResultadoDesempeno] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[SolicitadoPor] [int] NOT NULL,
	[FechaSolicitud] [datetime2](7) NULL,
	[AprobadoCE] [bit] NOT NULL,
	[UsuarioAprobacionCE] [int] NULL,
	[FechaAprobacionCE] [datetime2](7) NULL,
	[ComentarioCE] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AprobadoCMI] [bit] NOT NULL,
	[UsuarioAprobacionCMI] [int] NULL,
	[FechaAprobacionCMI] [datetime2](7) NULL,
	[ComentarioCMI] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Rechazado] [bit] NOT NULL,
	[UsuarioRechazo] [int] NULL,
	[FechaRechazo] [datetime2](7) NULL,
	[MotivoRechazo] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Estado] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[FechaCreacion] [datetime2](7) NULL,
	[FechaModificacion] [datetime2](7) NULL,
	[RowVersion] [timestamp] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdExcepcion] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
SET ANSI_PADDING ON

CREATE NONCLUSTERED INDEX [IX_Excepciones_Iglesia_Temporada] ON [dbo].[ExcepcionesRegla3Anios]
(
	[IdIglesia] ASC,
	[IdTemporada] ASC,
	[Estado] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Excepcion_Iglesia_Temporada_Activa] ON [dbo].[ExcepcionesRegla3Anios]
(
	[IdIglesia] ASC,
	[IdTemporada] ASC
)
WHERE ([Estado] IN ('PENDIENTE', 'APROBADA'))
WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[ExcepcionesRegla3Anios] ADD  DEFAULT ((1)) FOR [DiferenciaTemporadas]
ALTER TABLE [dbo].[ExcepcionesRegla3Anios] ADD  DEFAULT (getdate()) FOR [FechaSolicitud]
ALTER TABLE [dbo].[ExcepcionesRegla3Anios] ADD  DEFAULT ((0)) FOR [AprobadoCE]
ALTER TABLE [dbo].[ExcepcionesRegla3Anios] ADD  DEFAULT ((0)) FOR [AprobadoCMI]
ALTER TABLE [dbo].[ExcepcionesRegla3Anios] ADD  DEFAULT ((0)) FOR [Rechazado]
ALTER TABLE [dbo].[ExcepcionesRegla3Anios] ADD  DEFAULT ('PENDIENTE') FOR [Estado]
ALTER TABLE [dbo].[ExcepcionesRegla3Anios] ADD  DEFAULT (getdate()) FOR [FechaCreacion]
ALTER TABLE [dbo].[ExcepcionesRegla3Anios]  WITH CHECK ADD FOREIGN KEY([IdIglesia])
REFERENCES [dbo].[Iglesias] ([IdIglesia])
ALTER TABLE [dbo].[ExcepcionesRegla3Anios]  WITH CHECK ADD FOREIGN KEY([IdTemporada])
REFERENCES [dbo].[Temporadas] ([IdTemporada])
ALTER TABLE [dbo].[ExcepcionesRegla3Anios]  WITH CHECK ADD FOREIGN KEY([SolicitadoPor])
REFERENCES [dbo].[Usuarios] ([IdUsuario])
ALTER TABLE [dbo].[ExcepcionesRegla3Anios]  WITH CHECK ADD FOREIGN KEY([TemporadaPreviaId])
REFERENCES [dbo].[Temporadas] ([IdTemporada])
ALTER TABLE [dbo].[ExcepcionesRegla3Anios]  WITH CHECK ADD FOREIGN KEY([UsuarioAprobacionCE])
REFERENCES [dbo].[Usuarios] ([IdUsuario])
ALTER TABLE [dbo].[ExcepcionesRegla3Anios]  WITH CHECK ADD FOREIGN KEY([UsuarioAprobacionCMI])
REFERENCES [dbo].[Usuarios] ([IdUsuario])
ALTER TABLE [dbo].[ExcepcionesRegla3Anios]  WITH CHECK ADD FOREIGN KEY([UsuarioRechazo])
REFERENCES [dbo].[Usuarios] ([IdUsuario])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Finanzas_Categorias](
	[CategoriaId] [varchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Tipo] [varchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Grupo] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Descripcion] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Orden] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[CategoriaId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
INSERT [dbo].[Finanzas_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'E-0', N'GASTO', N'ENTRENAMIENTO', N'Envío, Retiro o Transferencia para Entrenamientos', 5)
INSERT [dbo].[Finanzas_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'E-1', N'GASTO', N'ENTRENAMIENTO', N'Transporte', 6)
INSERT [dbo].[Finanzas_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'E-2', N'GASTO', N'ENTRENAMIENTO', N'Snacks o Refrigerios', 7)
INSERT [dbo].[Finanzas_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'E-3', N'GASTO', N'ENTRENAMIENTO', N'Alimento', 8)
INSERT [dbo].[Finanzas_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'E-4', N'GASTO', N'ENTRENAMIENTO', N'Administración y Otros Gastos de Oficina', 9)
INSERT [dbo].[Finanzas_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'I-1', N'INGRESO', N'INGRESOS', N'Subvención - Entrenamientos', 1)
INSERT [dbo].[Finanzas_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'I-2', N'INGRESO', N'INGRESOS', N'Subvención - Mentoreo', 2)
INSERT [dbo].[Finanzas_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'I-3', N'INGRESO', N'INGRESOS', N'Ingresos para Logística', 3)
INSERT [dbo].[Finanzas_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'I-4', N'INGRESO', N'INGRESOS', N'Otros Ingresos', 4)
INSERT [dbo].[Finanzas_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'L-1', N'GASTO', N'LOGISTICA', N'Transporte de Cajitas y Literatura', 15)
INSERT [dbo].[Finanzas_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'L-2', N'GASTO', N'LOGISTICA', N'Almacenaje de Cajitas y Literatura', 16)
INSERT [dbo].[Finanzas_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'L-3', N'GASTO', N'LOGISTICA', N'Otros Gastos de Logística', 17)
INSERT [dbo].[Finanzas_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'M-0', N'GASTO', N'MENTOREO', N'Envío, Retiro o Transferencia para Mentoreo', 10)
INSERT [dbo].[Finanzas_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'M-1', N'GASTO', N'MENTOREO', N'Transporte', 11)
INSERT [dbo].[Finanzas_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'M-2', N'GASTO', N'MENTOREO', N'Alimento', 12)
INSERT [dbo].[Finanzas_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'M-3', N'GASTO', N'MENTOREO', N'Hospedaje', 13)
INSERT [dbo].[Finanzas_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'M-4', N'GASTO', N'MENTOREO', N'Administración y Otros Gastos de Oficina', 14)
INSERT [dbo].[Finanzas_Categorias] ([CategoriaId], [Tipo], [Grupo], [Descripcion], [Orden]) VALUES (N'O-1', N'GASTO', N'OTROS', N'Otros eventos o gastos aprobados', 18)
ALTER TABLE [dbo].[Finanzas_Categorias] ADD  DEFAULT ((0)) FOR [Orden]
ALTER TABLE [dbo].[Finanzas_Categorias]  WITH CHECK ADD CHECK  (([Tipo]='GASTO' OR [Tipo]='INGRESO'))

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Finanzas_PresupuestosAprobados](
	[PresupuestoId] [int] IDENTITY(1,1) NOT NULL,
	[IdTemporada] [int] NOT NULL,
	[IdEquipo] [int] NOT NULL,
	[CategoriaId] [varchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[MontoAprobadoUSD] [decimal](18, 2) NOT NULL,
	[MontoAprobadoDOP]  AS (CONVERT([decimal](18,2),round([MontoAprobadoUSD]*(58.63),(2)))),
PRIMARY KEY CLUSTERED 
(
	[PresupuestoId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
SET ANSI_PADDING ON

ALTER TABLE [dbo].[Finanzas_PresupuestosAprobados] ADD  CONSTRAINT [UQ_Finanzas_Presupuesto] UNIQUE NONCLUSTERED 
(
	[IdTemporada] ASC,
	[IdEquipo] ASC,
	[CategoriaId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[Finanzas_PresupuestosAprobados] ADD  DEFAULT ((0)) FOR [MontoAprobadoUSD]
ALTER TABLE [dbo].[Finanzas_PresupuestosAprobados]  WITH CHECK ADD FOREIGN KEY([CategoriaId])
REFERENCES [dbo].[Finanzas_Categorias] ([CategoriaId])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Finanzas_Transacciones](
	[TransaccionId] [bigint] IDENTITY(1,1) NOT NULL,
	[IdTemporada] [int] NOT NULL,
	[IdEquipo] [int] NOT NULL,
	[Mes] [varchar](3) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Fecha] [date] NOT NULL,
	[NumeroDocumento] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Descripcion] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CategoriaId] [varchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[GastoDOP] [decimal](18, 2) NOT NULL,
	[IngresoDOP] [decimal](18, 2) NOT NULL,
	[TasaCambio] [decimal](10, 4) NOT NULL,
	[GastoUSD]  AS (CONVERT([decimal](18,2),round([GastoDOP]/nullif([TasaCambio],(0)),(2)))),
	[IngresoUSD]  AS (CONVERT([decimal](18,2),round([IngresoDOP]/nullif([TasaCambio],(0)),(2)))),
	[Notas] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FechaCreacion] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[TransaccionId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[Finanzas_Transacciones] ADD  DEFAULT ((0)) FOR [GastoDOP]
ALTER TABLE [dbo].[Finanzas_Transacciones] ADD  DEFAULT ((0)) FOR [IngresoDOP]
ALTER TABLE [dbo].[Finanzas_Transacciones] ADD  DEFAULT ((58.63)) FOR [TasaCambio]
ALTER TABLE [dbo].[Finanzas_Transacciones] ADD  DEFAULT (getdate()) FOR [FechaCreacion]
ALTER TABLE [dbo].[Finanzas_Transacciones]  WITH CHECK ADD FOREIGN KEY([CategoriaId])
REFERENCES [dbo].[Finanzas_Categorias] ([CategoriaId])
ALTER TABLE [dbo].[Finanzas_Transacciones]  WITH CHECK ADD CHECK  (([Mes]='AGO' OR [Mes]='JUL' OR [Mes]='JUN' OR [Mes]='MAY' OR [Mes]='ABR' OR [Mes]='MAR' OR [Mes]='FEB' OR [Mes]='ENE' OR [Mes]='DIC' OR [Mes]='NOV' OR [Mes]='OCT' OR [Mes]='SEP'))

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[HistorialParticipacion](
	[IdHistorial] [int] IDENTITY(1,1) NOT NULL,
	[IdParticipacion] [int] NOT NULL,
	[FechaHora] [datetime] NULL,
	[AccionRealizada] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[EstadoAnterior] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[EstadoNuevo] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IdUsuarioResponsable] [int] NOT NULL,
	[Comentario] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Razon] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[IdHistorial] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
SET IDENTITY_INSERT [dbo].[HistorialParticipacion] ON 

INSERT [dbo].[HistorialParticipacion] ([IdHistorial], [IdParticipacion], [FechaHora], [AccionRealizada], [EstadoAnterior], [EstadoNuevo], [IdUsuarioResponsable], [Comentario], [Razon]) VALUES (44, 1, CAST(N'2026-08-24T22:05:01.697' AS DateTime), N'Inscripción en Temporada', NULL, N'Inscrita (Etapa 1)', 1, N'Iglesia inscrita exitosamente en la temporada activa.', NULL)
INSERT [dbo].[HistorialParticipacion] ([IdHistorial], [IdParticipacion], [FechaHora], [AccionRealizada], [EstadoAnterior], [EstadoNuevo], [IdUsuarioResponsable], [Comentario], [Razon]) VALUES (45, 2, CAST(N'2026-08-24T22:11:38.407' AS DateTime), N'Inscripción en Temporada', NULL, N'Inscrita (Etapa 1)', 1, N'Iglesia inscrita exitosamente en la temporada activa.', NULL)
INSERT [dbo].[HistorialParticipacion] ([IdHistorial], [IdParticipacion], [FechaHora], [AccionRealizada], [EstadoAnterior], [EstadoNuevo], [IdUsuarioResponsable], [Comentario], [Razon]) VALUES (46, 2, CAST(N'2026-08-24T22:26:37.160' AS DateTime), N'Evaluación Inicial', N'Inscrita (Etapa 1)', N'Evaluada (Etapa 2)', 2, N'Evaluación inicial APROBADA. Avanza a Evaluada Inicial (Etapa 2).', NULL)
INSERT [dbo].[HistorialParticipacion] ([IdHistorial], [IdParticipacion], [FechaHora], [AccionRealizada], [EstadoAnterior], [EstadoNuevo], [IdUsuarioResponsable], [Comentario], [Razon]) VALUES (47, 2, CAST(N'2026-08-24T23:21:41.750' AS DateTime), N'Asignación de Visión', N'Evaluada Inicial (Etapa 2)', N'Visión (Etapa 3)', 2, N'Se asignó el evento de Presentación de la Visión y se confirmaron/actualizaron los datos del Pastor y Líder.', NULL)
INSERT [dbo].[HistorialParticipacion] ([IdHistorial], [IdParticipacion], [FechaHora], [AccionRealizada], [EstadoAnterior], [EstadoNuevo], [IdUsuarioResponsable], [Comentario], [Razon]) VALUES (48, 2, CAST(N'2026-08-24T23:27:10.500' AS DateTime), N'Aprobación Elegibilidad Taller', N'Visión (Etapa 3)', N'Elegible Taller (Etapa 4)', 2, N'El CMI/CE aprobó la elegibilidad de la iglesia. Asistencia a Visión confirmada. Iglesia elegible para Taller OCC.', NULL)
INSERT [dbo].[HistorialParticipacion] ([IdHistorial], [IdParticipacion], [FechaHora], [AccionRealizada], [EstadoAnterior], [EstadoNuevo], [IdUsuarioResponsable], [Comentario], [Razon]) VALUES (49, 2, CAST(N'2026-08-24T23:39:56.797' AS DateTime), N'Maestro Eliminado: Maria LA Del Barrio', NULL, NULL, 3, N'Maestro Eliminado: Maria LA Del Barrio. cambio deamesto', NULL)
INSERT [dbo].[HistorialParticipacion] ([IdHistorial], [IdParticipacion], [FechaHora], [AccionRealizada], [EstadoAnterior], [EstadoNuevo], [IdUsuarioResponsable], [Comentario], [Razon]) VALUES (50, 2, CAST(N'2026-08-24T23:41:02.617' AS DateTime), N'Elegibilidad Taller', N'Presentación Visión (Etapa 3)', N'Taller OCC (Etapa 5)', 3, N'Elegibilidad de Taller aprobada. Habilitada para Taller OCC (Etapa 5).', NULL)
INSERT [dbo].[HistorialParticipacion] ([IdHistorial], [IdParticipacion], [FechaHora], [AccionRealizada], [EstadoAnterior], [EstadoNuevo], [IdUsuarioResponsable], [Comentario], [Razon]) VALUES (51, 2, CAST(N'2026-08-24T23:43:54.227' AS DateTime), N'Completado Taller OCC', N'Taller OCC (Etapa 5)', N'Evaluación Asignación (Etapa 6)', 3, N'Asistencia al Taller OCC confirmada. Avanza a Evaluación Asignación.', NULL)
INSERT [dbo].[HistorialParticipacion] ([IdHistorial], [IdParticipacion], [FechaHora], [AccionRealizada], [EstadoAnterior], [EstadoNuevo], [IdUsuarioResponsable], [Comentario], [Razon]) VALUES (52, 2, CAST(N'2026-09-02T04:24:23.930' AS DateTime), N'Asignación de Recursos Finalizada', N'Evaluación Asignación (Etapa 6)', N'Aprobación Final (Etapa 7)', 1, N'Se finalizó la asignación de recursos y se completó la participación.', NULL)
INSERT [dbo].[HistorialParticipacion] ([IdHistorial], [IdParticipacion], [FechaHora], [AccionRealizada], [EstadoAnterior], [EstadoNuevo], [IdUsuarioResponsable], [Comentario], [Razon]) VALUES (53, 2, CAST(N'2026-09-02T05:13:33.123' AS DateTime), N'Edición de Iglesia', N'Aprobado', N'Aprobado', 1, N'Datos de la iglesia actualizados por el usuario.', NULL)
INSERT [dbo].[HistorialParticipacion] ([IdHistorial], [IdParticipacion], [FechaHora], [AccionRealizada], [EstadoAnterior], [EstadoNuevo], [IdUsuarioResponsable], [Comentario], [Razon]) VALUES (54, 3, CAST(N'2026-09-02T16:50:34.240' AS DateTime), N'Inscripción en Temporada', NULL, N'Inscrita (Etapa 1)', 1, N'Iglesia inscrita exitosamente en la temporada activa.', NULL)
INSERT [dbo].[HistorialParticipacion] ([IdHistorial], [IdParticipacion], [FechaHora], [AccionRealizada], [EstadoAnterior], [EstadoNuevo], [IdUsuarioResponsable], [Comentario], [Razon]) VALUES (55, 3, CAST(N'2026-09-02T16:51:38.647' AS DateTime), N'Evaluación Inicial', N'Inscrita (Etapa 1)', N'Evaluada (Etapa 2)', 1, N'Evaluación inicial APROBADA. Avanza a Evaluada Inicial (Etapa 2).', NULL)
INSERT [dbo].[HistorialParticipacion] ([IdHistorial], [IdParticipacion], [FechaHora], [AccionRealizada], [EstadoAnterior], [EstadoNuevo], [IdUsuarioResponsable], [Comentario], [Razon]) VALUES (56, 3, CAST(N'2026-09-02T16:58:23.727' AS DateTime), N'Asignación de Visión', N'Evaluada Inicial (Etapa 2)', N'Visión (Etapa 3)', 1, N'Se asignó el evento de Presentación de la Visión y se confirmaron/actualizaron los datos del Pastor y Líder.', NULL)
INSERT [dbo].[HistorialParticipacion] ([IdHistorial], [IdParticipacion], [FechaHora], [AccionRealizada], [EstadoAnterior], [EstadoNuevo], [IdUsuarioResponsable], [Comentario], [Razon]) VALUES (57, 3, CAST(N'2026-09-02T16:59:37.573' AS DateTime), N'Aprobación Elegibilidad Taller', N'Visión (Etapa 3)', N'Elegible Taller (Etapa 4)', 1, N'El CMI/CE aprobó la elegibilidad de la iglesia. Asistencia a Visión confirmada. Iglesia elegible para Taller OCC.', NULL)
INSERT [dbo].[HistorialParticipacion] ([IdHistorial], [IdParticipacion], [FechaHora], [AccionRealizada], [EstadoAnterior], [EstadoNuevo], [IdUsuarioResponsable], [Comentario], [Razon]) VALUES (58, 3, CAST(N'2026-09-02T17:10:36.057' AS DateTime), N'Elegibilidad Taller', N'Presentación Visión (Etapa 3)', N'Taller OCC (Etapa 5)', 1, N'Elegibilidad de Taller aprobada. Habilitada para Taller OCC (Etapa 5).', NULL)
INSERT [dbo].[HistorialParticipacion] ([IdHistorial], [IdParticipacion], [FechaHora], [AccionRealizada], [EstadoAnterior], [EstadoNuevo], [IdUsuarioResponsable], [Comentario], [Razon]) VALUES (59, 3, CAST(N'2026-09-02T17:11:31.963' AS DateTime), N'Completado Taller OCC', N'Taller OCC (Etapa 5)', N'Evaluación Asignación (Etapa 6)', 1, N'Asistencia al Taller OCC confirmada. Avanza a Evaluación Asignación.', NULL)
INSERT [dbo].[HistorialParticipacion] ([IdHistorial], [IdParticipacion], [FechaHora], [AccionRealizada], [EstadoAnterior], [EstadoNuevo], [IdUsuarioResponsable], [Comentario], [Razon]) VALUES (60, 3, CAST(N'2026-09-02T18:01:24.000' AS DateTime), N'Asignación de Recursos Finalizada', N'Evaluación Asignación (Etapa 6)', N'Aprobación Final (Etapa 7)', 1, N'Se finalizó la asignación de recursos y se completó la participación.', NULL)
SET IDENTITY_INSERT [dbo].[HistorialParticipacion] OFF
ALTER TABLE [dbo].[HistorialParticipacion] ADD  DEFAULT (getdate()) FOR [FechaHora]
ALTER TABLE [dbo].[HistorialParticipacion]  WITH CHECK ADD FOREIGN KEY([IdParticipacion])
REFERENCES [dbo].[ParticipacionesIglesia] ([IdParticipacion])
ALTER TABLE [dbo].[HistorialParticipacion]  WITH CHECK ADD FOREIGN KEY([IdUsuarioResponsable])
REFERENCES [dbo].[Usuarios] ([IdUsuario])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Iglesias](
	[IdIglesia] [int] IDENTITY(1,1) NOT NULL,
	[NombreIglesia] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[RNC_Cedula] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Telefono] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Calle] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Numero] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Sector] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Ciudad] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Provincia] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Referencia] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IdEquipo] [int] NOT NULL,
	[IdUsuarioCreacion] [int] NULL,
	[FechaCreacion] [datetime] NULL,
	[Denominacion] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TipoOrganizacion] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CantidadMaestros] [int] NULL,
	[CantidadNinos] [int] NULL,
	[Ref1Nombre] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Ref1Contacto] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Ref2Nombre] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Ref2Contacto] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CorreoInstitucion] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[RowVersion] [timestamp] NOT NULL,
	[FechaModificacion] [datetime2](7) NULL,
	[UsuarioModificacion] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdIglesia] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
SET IDENTITY_INSERT [dbo].[Iglesias] ON 

INSERT [dbo].[Iglesias] ([IdIglesia], [NombreIglesia], [RNC_Cedula], [Telefono], [Calle], [Numero], [Sector], [Ciudad], [Provincia], [Referencia], [IdEquipo], [IdUsuarioCreacion], [FechaCreacion], [Denominacion], [TipoOrganizacion], [CantidadMaestros], [CantidadNinos], [Ref1Nombre], [Ref1Contacto], [Ref2Nombre], [Ref2Contacto], [CorreoInstitucion], [FechaModificacion], [UsuarioModificacion]) VALUES (1, N'Iglesia Ejemplo de Fe', N'123456785', N'8094568574', N'Calle Principal', N'1', N'Los Peralejos', N'Distrito Nacional', N'Santo Domingo', N'Cerca del Parque Los Cocos', 2, 1, CAST(N'2026-08-24T22:05:01.687' AS DateTime), N'Asamblea de Dios', N'Iglesia', 5, 50, NULL, NULL, NULL, NULL, N'inglesia.institucion@correoejemplo.com', NULL, NULL)
INSERT [dbo].[Iglesias] ([IdIglesia], [NombreIglesia], [RNC_Cedula], [Telefono], [Calle], [Numero], [Sector], [Ciudad], [Provincia], [Referencia], [IdEquipo], [IdUsuarioCreacion], [FechaCreacion], [Denominacion], [TipoOrganizacion], [CantidadMaestros], [CantidadNinos], [Ref1Nombre], [Ref1Contacto], [Ref2Nombre], [Ref2Contacto], [CorreoInstitucion], [FechaModificacion], [UsuarioModificacion]) VALUES (2, N'Iglesia Un Templo', N'132290526', N'8092323518', N'10', N'15', N'Los peralejos', N'Santo Domingo', N'Santo Domingo', N'Frente a Una casa', 2, 1, CAST(N'2026-08-24T22:11:38.403' AS DateTime), N'Iglesia de Dios', N'Iglesia Local', 3, 120, N'Referencia Polo', N'8495632541', N'Referencia Apolo', N'8298527812', N'correo@correo.com', CAST(N'2026-09-02T09:13:33.1100000' AS DateTime2), 1)
INSERT [dbo].[Iglesias] ([IdIglesia], [NombreIglesia], [RNC_Cedula], [Telefono], [Calle], [Numero], [Sector], [Ciudad], [Provincia], [Referencia], [IdEquipo], [IdUsuarioCreacion], [FechaCreacion], [Denominacion], [TipoOrganizacion], [CantidadMaestros], [CantidadNinos], [Ref1Nombre], [Ref1Contacto], [Ref2Nombre], [Ref2Contacto], [CorreoInstitucion], [FechaModificacion], [UsuarioModificacion]) VALUES (3, N'Iglesia Real Camino', N'40245887895', N'8092323518', N'10', N'15', N'Los peralejos', N'Santo Domingo', N'Santo Domingo', N'Frente a Una casa', 24, 1, CAST(N'2026-09-02T16:50:34.223' AS DateTime), N'Bautista', N'Iglesia Local', 2, 12, N'Referencia Polo', N'8499667845', N'Referencia Apolo', N'8298527812', N'correo@correo.com', NULL, NULL)
SET IDENTITY_INSERT [dbo].[Iglesias] OFF
CREATE NONCLUSTERED INDEX [IX_Iglesias_IdEquipo] ON [dbo].[Iglesias]
(
	[IdEquipo] ASC
)
INCLUDE([NombreIglesia],[RNC_Cedula],[Telefono],[Denominacion],[TipoOrganizacion]) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
SET ANSI_PADDING ON

CREATE NONCLUSTERED INDEX [IX_Iglesias_RNC_Cedula] ON [dbo].[Iglesias]
(
	[RNC_Cedula] ASC
)
WHERE ([RNC_Cedula] IS NOT NULL)
WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[Iglesias] ADD  DEFAULT (getdate()) FOR [FechaCreacion]
ALTER TABLE [dbo].[Iglesias]  WITH CHECK ADD FOREIGN KEY([IdEquipo])
REFERENCES [dbo].[Equipos] ([IdEquipo])
ALTER TABLE [dbo].[Iglesias]  WITH CHECK ADD FOREIGN KEY([IdUsuarioCreacion])
REFERENCES [dbo].[Usuarios] ([IdUsuario])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[InventarioCentral](
	[IdInventarioCentral] [int] IDENTITY(1,1) NOT NULL,
	[IdTemporada] [int] NOT NULL,
	[IdAlmacen] [int] NOT NULL,
	[IdMaterial] [int] NOT NULL,
	[CantidadFisica] [int] NOT NULL,
	[CantidadTransferida] [int] NOT NULL,
	[CantidadDisponible] [int] NOT NULL,
	[FechaActualizacion] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdInventarioCentral] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET IDENTITY_INSERT [dbo].[InventarioCentral] ON 

INSERT [dbo].[InventarioCentral] ([IdInventarioCentral], [IdTemporada], [IdAlmacen], [IdMaterial], [CantidadFisica], [CantidadTransferida], [CantidadDisponible], [FechaActualizacion]) VALUES (1, 1, 1, 1, 600, 120, 480, CAST(N'2026-09-02T02:55:11.8733333' AS DateTime2))
INSERT [dbo].[InventarioCentral] ([IdInventarioCentral], [IdTemporada], [IdAlmacen], [IdMaterial], [CantidadFisica], [CantidadTransferida], [CantidadDisponible], [FechaActualizacion]) VALUES (2, 1, 1, 3, 320, 60, 260, CAST(N'2026-09-02T03:28:46.1466667' AS DateTime2))
INSERT [dbo].[InventarioCentral] ([IdInventarioCentral], [IdTemporada], [IdAlmacen], [IdMaterial], [CantidadFisica], [CantidadTransferida], [CantidadDisponible], [FechaActualizacion]) VALUES (3, 1, 2, 3, 21750, 14400, 7350, CAST(N'2026-09-02T15:29:30.6300000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[InventarioCentral] OFF
ALTER TABLE [dbo].[InventarioCentral] ADD  CONSTRAINT [UQ_InventarioCentral_TempAlmMat] UNIQUE NONCLUSTERED 
(
	[IdTemporada] ASC,
	[IdAlmacen] ASC,
	[IdMaterial] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[InventarioCentral] ADD  DEFAULT ((0)) FOR [CantidadFisica]
ALTER TABLE [dbo].[InventarioCentral] ADD  DEFAULT ((0)) FOR [CantidadTransferida]
ALTER TABLE [dbo].[InventarioCentral] ADD  DEFAULT ((0)) FOR [CantidadDisponible]
ALTER TABLE [dbo].[InventarioCentral] ADD  DEFAULT (getdate()) FOR [FechaActualizacion]
ALTER TABLE [dbo].[InventarioCentral]  WITH CHECK ADD FOREIGN KEY([IdAlmacen])
REFERENCES [dbo].[Almacenes] ([IdAlmacen])
ALTER TABLE [dbo].[InventarioCentral]  WITH CHECK ADD FOREIGN KEY([IdMaterial])
REFERENCES [dbo].[Materiales] ([IdMaterial])
ALTER TABLE [dbo].[InventarioCentral]  WITH CHECK ADD FOREIGN KEY([IdTemporada])
REFERENCES [dbo].[Temporadas] ([IdTemporada])
ALTER TABLE [dbo].[InventarioCentral]  WITH CHECK ADD  CONSTRAINT [CK_InventarioCentral_Disp] CHECK  (([CantidadDisponible]>=(0)))
ALTER TABLE [dbo].[InventarioCentral] CHECK CONSTRAINT [CK_InventarioCentral_Disp]

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[InventarioEquipo](
	[IdInventarioEquipo] [int] IDENTITY(1,1) NOT NULL,
	[IdTemporada] [int] NOT NULL,
	[IdEquipo] [int] NOT NULL,
	[IdMaterial] [int] NOT NULL,
	[CantidadRecibida] [int] NOT NULL,
	[CantidadAsignada] [int] NOT NULL,
	[CantidadDespachada] [int] NOT NULL,
	[CantidadDisponible] [int] NOT NULL,
	[FechaActualizacion] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdInventarioEquipo] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET IDENTITY_INSERT [dbo].[InventarioEquipo] ON 

INSERT [dbo].[InventarioEquipo] ([IdInventarioEquipo], [IdTemporada], [IdEquipo], [IdMaterial], [CantidadRecibida], [CantidadAsignada], [CantidadDespachada], [CantidadDisponible], [FechaActualizacion]) VALUES (1, 1, 1, 1, 120, 0, 0, 120, CAST(N'2026-09-02T02:55:11.8733333' AS DateTime2))
INSERT [dbo].[InventarioEquipo] ([IdInventarioEquipo], [IdTemporada], [IdEquipo], [IdMaterial], [CantidadRecibida], [CantidadAsignada], [CantidadDespachada], [CantidadDisponible], [FechaActualizacion]) VALUES (2, 1, 1, 3, 60, 50, 50, 10, CAST(N'2026-09-02T03:29:28.4400000' AS DateTime2))
INSERT [dbo].[InventarioEquipo] ([IdInventarioEquipo], [IdTemporada], [IdEquipo], [IdMaterial], [CantidadRecibida], [CantidadAsignada], [CantidadDespachada], [CantidadDisponible], [FechaActualizacion]) VALUES (3, 1, 24, 3, 7510, 100, 100, 7410, CAST(N'2026-09-02T19:26:31.1866667' AS DateTime2))
INSERT [dbo].[InventarioEquipo] ([IdInventarioEquipo], [IdTemporada], [IdEquipo], [IdMaterial], [CantidadRecibida], [CantidadAsignada], [CantidadDespachada], [CantidadDisponible], [FechaActualizacion]) VALUES (4, 1, 2, 3, 14400, 0, 0, 14400, CAST(N'2026-09-02T19:46:56.9600000' AS DateTime2))
INSERT [dbo].[InventarioEquipo] ([IdInventarioEquipo], [IdTemporada], [IdEquipo], [IdMaterial], [CantidadRecibida], [CantidadAsignada], [CantidadDespachada], [CantidadDisponible], [FechaActualizacion]) VALUES (5, 1, 24, 1, 600, 0, 0, 600, CAST(N'2026-09-02T20:42:48.9333333' AS DateTime2))
SET IDENTITY_INSERT [dbo].[InventarioEquipo] OFF
ALTER TABLE [dbo].[InventarioEquipo] ADD  CONSTRAINT [UQ_InventarioEquipo_TempEqMat] UNIQUE NONCLUSTERED 
(
	[IdTemporada] ASC,
	[IdEquipo] ASC,
	[IdMaterial] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[InventarioEquipo] ADD  DEFAULT ((0)) FOR [CantidadRecibida]
ALTER TABLE [dbo].[InventarioEquipo] ADD  DEFAULT ((0)) FOR [CantidadAsignada]
ALTER TABLE [dbo].[InventarioEquipo] ADD  DEFAULT ((0)) FOR [CantidadDespachada]
ALTER TABLE [dbo].[InventarioEquipo] ADD  DEFAULT ((0)) FOR [CantidadDisponible]
ALTER TABLE [dbo].[InventarioEquipo] ADD  DEFAULT (getdate()) FOR [FechaActualizacion]
ALTER TABLE [dbo].[InventarioEquipo]  WITH CHECK ADD FOREIGN KEY([IdEquipo])
REFERENCES [dbo].[Equipos] ([IdEquipo])
ALTER TABLE [dbo].[InventarioEquipo]  WITH CHECK ADD FOREIGN KEY([IdMaterial])
REFERENCES [dbo].[Materiales] ([IdMaterial])
ALTER TABLE [dbo].[InventarioEquipo]  WITH CHECK ADD FOREIGN KEY([IdTemporada])
REFERENCES [dbo].[Temporadas] ([IdTemporada])
ALTER TABLE [dbo].[InventarioEquipo]  WITH CHECK ADD  CONSTRAINT [CK_InventarioEquipo_Disp] CHECK  (([CantidadDisponible]>=(0)))
ALTER TABLE [dbo].[InventarioEquipo] CHECK CONSTRAINT [CK_InventarioEquipo_Disp]

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[LogsCambiosEtapa](
	[IdLog] [int] IDENTITY(1,1) NOT NULL,
	[IdIglesia] [int] NOT NULL,
	[EtapaAnterior] [int] NOT NULL,
	[EtapaNueva] [int] NOT NULL,
	[IdUsuarioResponsable] [int] NOT NULL,
	[FechaHora] [datetime] NULL,
	[Detalles] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[IdLog] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET IDENTITY_INSERT [dbo].[LogsCambiosEtapa] ON 

INSERT [dbo].[LogsCambiosEtapa] ([IdLog], [IdIglesia], [EtapaAnterior], [EtapaNueva], [IdUsuarioResponsable], [FechaHora], [Detalles]) VALUES (5, 2, 5, 6, 3, CAST(N'2026-08-24T23:43:54.227' AS DateTime), N'Transición automática tras registrar asistencia en Taller OCC.')
INSERT [dbo].[LogsCambiosEtapa] ([IdLog], [IdIglesia], [EtapaAnterior], [EtapaNueva], [IdUsuarioResponsable], [FechaHora], [Detalles]) VALUES (6, 2, 6, 7, 1, CAST(N'2026-09-02T04:24:23.930' AS DateTime), N'Transición de asignación final de recursos y cierre de participación.')
INSERT [dbo].[LogsCambiosEtapa] ([IdLog], [IdIglesia], [EtapaAnterior], [EtapaNueva], [IdUsuarioResponsable], [FechaHora], [Detalles]) VALUES (7, 3, 5, 6, 1, CAST(N'2026-09-02T17:11:31.963' AS DateTime), N'Transición automática tras registrar asistencia en Taller OCC.')
INSERT [dbo].[LogsCambiosEtapa] ([IdLog], [IdIglesia], [EtapaAnterior], [EtapaNueva], [IdUsuarioResponsable], [FechaHora], [Detalles]) VALUES (8, 3, 6, 7, 1, CAST(N'2026-09-02T18:01:24.000' AS DateTime), N'Transición de asignación final de recursos y cierre de participación.')
SET IDENTITY_INSERT [dbo].[LogsCambiosEtapa] OFF
ALTER TABLE [dbo].[LogsCambiosEtapa] ADD  DEFAULT (getdate()) FOR [FechaHora]

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Maestros](
	[IdMaestro] [int] IDENTITY(1,1) NOT NULL,
	[IdIglesia] [int] NOT NULL,
	[Nombres] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Apellidos] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[DocumentoIdentidad] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Celular] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Correo] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Activo] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdMaestro] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
SET IDENTITY_INSERT [dbo].[Maestros] ON 

INSERT [dbo].[Maestros] ([IdMaestro], [IdIglesia], [Nombres], [Apellidos], [DocumentoIdentidad], [Celular], [Correo], [Activo]) VALUES (2, 2, N'Maria', N'LA Del Barrio', N'40225455856', N'8092323518', N'erlegsd.occrd@gmail.com', 1)
INSERT [dbo].[Maestros] ([IdMaestro], [IdIglesia], [Nombres], [Apellidos], [DocumentoIdentidad], [Celular], [Correo], [Activo]) VALUES (3, 2, N'Servicio', N'Al Cliente', N'40256998524', N'8095624578', N'portaforza@gmail.com', 1)
INSERT [dbo].[Maestros] ([IdMaestro], [IdIglesia], [Nombres], [Apellidos], [DocumentoIdentidad], [Celular], [Correo], [Activo]) VALUES (4, 3, N'Sergio', N'Torres', N'40256998524', N'8095624578', N'portaforza@gmail.com', 1)
SET IDENTITY_INSERT [dbo].[Maestros] OFF
CREATE NONCLUSTERED INDEX [IX_Maestros_IdIglesia_Activo] ON [dbo].[Maestros]
(
	[IdIglesia] ASC,
	[Activo] ASC
)
INCLUDE([Nombres],[Apellidos],[DocumentoIdentidad],[Celular],[Correo]) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[Maestros] ADD  DEFAULT ((1)) FOR [Activo]
ALTER TABLE [dbo].[Maestros]  WITH CHECK ADD FOREIGN KEY([IdIglesia])
REFERENCES [dbo].[Iglesias] ([IdIglesia])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Materiales](
	[IdMaterial] [int] IDENTITY(1,1) NOT NULL,
	[Codigo] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[NombreMaterial] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[UnidadEntrega] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[MomentoEntrega] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Activo] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdMaterial] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
SET IDENTITY_INSERT [dbo].[Materiales] ON 

INSERT [dbo].[Materiales] ([IdMaterial], [Codigo], [NombreMaterial], [UnidadEntrega], [MomentoEntrega], [Activo]) VALUES (1, N'GM', N'Guía del Maestro', N'Guía', N'DESPACHO', 1)
INSERT [dbo].[Materiales] ([IdMaterial], [Codigo], [NombreMaterial], [UnidadEntrega], [MomentoEntrega], [Activo]) VALUES (2, N'GA', N'Guías del Alumno', N'Guía', N'DESPACHO', 1)
INSERT [dbo].[Materiales] ([IdMaterial], [Codigo], [NombreMaterial], [UnidadEntrega], [MomentoEntrega], [Activo]) VALUES (3, N'OE', N'Oportunidades Evangelísticas', N'Cajita', N'DESPACHO', 1)
INSERT [dbo].[Materiales] ([IdMaterial], [Codigo], [NombreMaterial], [UnidadEntrega], [MomentoEntrega], [Activo]) VALUES (4, N'MR', N'El Mejor Regalo', N'Libro', N'DESPACHO', 1)
INSERT [dbo].[Materiales] ([IdMaterial], [Codigo], [NombreMaterial], [UnidadEntrega], [MomentoEntrega], [Activo]) VALUES (5, N'PO', N'Poster', N'Poster', N'DESPACHO', 1)
INSERT [dbo].[Materiales] ([IdMaterial], [Codigo], [NombreMaterial], [UnidadEntrega], [MomentoEntrega], [Activo]) VALUES (6, N'NT', N'Nuevos Testamentos', N'Ejemplar', N'DESPACHO', 1)
INSERT [dbo].[Materiales] ([IdMaterial], [Codigo], [NombreMaterial], [UnidadEntrega], [MomentoEntrega], [Activo]) VALUES (7, N'BR', N'Brochures', N'Brochure', N'PRESENTACION_VISION', 1)
SET IDENTITY_INSERT [dbo].[Materiales] OFF
SET ANSI_PADDING ON

ALTER TABLE [dbo].[Materiales] ADD UNIQUE NONCLUSTERED 
(
	[Codigo] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[Materiales] ADD  DEFAULT ((1)) FOR [Activo]

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[MovimientosInventario](
	[IdMovimiento] [int] IDENTITY(1,1) NOT NULL,
	[IdTemporada] [int] NOT NULL,
	[TipoMovimiento] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[IdMaterial] [int] NOT NULL,
	[Cantidad] [int] NOT NULL,
	[IdAlmacenOrigen] [int] NULL,
	[IdAlmacenDestino] [int] NULL,
	[IdEquipoDestino] [int] NULL,
	[IdIglesia] [int] NULL,
	[IdDocumentoReferencia] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FechaHora] [datetime2](7) NOT NULL,
	[IdUsuario] [int] NOT NULL,
	[Justificacion] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[IdMovimiento] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
SET IDENTITY_INSERT [dbo].[MovimientosInventario] ON 

INSERT [dbo].[MovimientosInventario] ([IdMovimiento], [IdTemporada], [TipoMovimiento], [IdMaterial], [Cantidad], [IdAlmacenOrigen], [IdAlmacenDestino], [IdEquipoDestino], [IdIglesia], [IdDocumentoReferencia], [FechaHora], [IdUsuario], [Justificacion]) VALUES (1, 1, N'RECEPCION_CONTENEDOR', 1, 200, NULL, 1, NULL, NULL, N'REC-1', CAST(N'2026-09-02T02:54:13.1766667' AS DateTime2), 1, N'RecepciÃ³n automatizada test')
INSERT [dbo].[MovimientosInventario] ([IdMovimiento], [IdTemporada], [TipoMovimiento], [IdMaterial], [Cantidad], [IdAlmacenOrigen], [IdAlmacenDestino], [IdEquipoDestino], [IdIglesia], [IdDocumentoReferencia], [FechaHora], [IdUsuario], [Justificacion]) VALUES (2, 1, N'TRANSFERENCIA_EQUIPO', 1, 40, 1, NULL, 1, NULL, N'TEST-CTE-3878', CAST(N'2026-09-02T02:54:13.2033333' AS DateTime2), 1, N'Transferencia a equipo test')
INSERT [dbo].[MovimientosInventario] ([IdMovimiento], [IdTemporada], [TipoMovimiento], [IdMaterial], [Cantidad], [IdAlmacenOrigen], [IdAlmacenDestino], [IdEquipoDestino], [IdIglesia], [IdDocumentoReferencia], [FechaHora], [IdUsuario], [Justificacion]) VALUES (3, 1, N'RECEPCION_CONTENEDOR', 1, 200, NULL, 1, NULL, NULL, N'REC-2', CAST(N'2026-09-02T02:54:50.9800000' AS DateTime2), 1, N'RecepciÃ³n automatizada test')
INSERT [dbo].[MovimientosInventario] ([IdMovimiento], [IdTemporada], [TipoMovimiento], [IdMaterial], [Cantidad], [IdAlmacenOrigen], [IdAlmacenDestino], [IdEquipoDestino], [IdIglesia], [IdDocumentoReferencia], [FechaHora], [IdUsuario], [Justificacion]) VALUES (4, 1, N'TRANSFERENCIA_EQUIPO', 1, 40, 1, NULL, 1, NULL, N'TEST-CTE-9213', CAST(N'2026-09-02T02:54:51.0000000' AS DateTime2), 1, N'Transferencia a equipo test')
INSERT [dbo].[MovimientosInventario] ([IdMovimiento], [IdTemporada], [TipoMovimiento], [IdMaterial], [Cantidad], [IdAlmacenOrigen], [IdAlmacenDestino], [IdEquipoDestino], [IdIglesia], [IdDocumentoReferencia], [FechaHora], [IdUsuario], [Justificacion]) VALUES (5, 1, N'RECEPCION_CONTENEDOR', 1, 200, NULL, 1, NULL, NULL, N'REC-3', CAST(N'2026-09-02T02:55:11.8500000' AS DateTime2), 1, N'RecepciÃ³n automatizada test')
INSERT [dbo].[MovimientosInventario] ([IdMovimiento], [IdTemporada], [TipoMovimiento], [IdMaterial], [Cantidad], [IdAlmacenOrigen], [IdAlmacenDestino], [IdEquipoDestino], [IdIglesia], [IdDocumentoReferencia], [FechaHora], [IdUsuario], [Justificacion]) VALUES (6, 1, N'TRANSFERENCIA_EQUIPO', 1, 40, 1, NULL, 1, NULL, N'TEST-CTE-7221', CAST(N'2026-09-02T02:55:11.8733333' AS DateTime2), 1, N'Transferencia a equipo test')
INSERT [dbo].[MovimientosInventario] ([IdMovimiento], [IdTemporada], [TipoMovimiento], [IdMaterial], [Cantidad], [IdAlmacenOrigen], [IdAlmacenDestino], [IdEquipoDestino], [IdIglesia], [IdDocumentoReferencia], [FechaHora], [IdUsuario], [Justificacion]) VALUES (7, 1, N'DESPACHO_IGLESIA', 1, 10, NULL, NULL, 2, 2, N'DSP-TEST-8363', CAST(N'2026-09-02T02:55:12.2733333' AS DateTime2), 1, N'Entrega confirmada a Pastor')
INSERT [dbo].[MovimientosInventario] ([IdMovimiento], [IdTemporada], [TipoMovimiento], [IdMaterial], [Cantidad], [IdAlmacenOrigen], [IdAlmacenDestino], [IdEquipoDestino], [IdIglesia], [IdDocumentoReferencia], [FechaHora], [IdUsuario], [Justificacion]) VALUES (8, 1, N'RECEPCION_CONTENEDOR', 3, 160, NULL, 1, NULL, NULL, N'REC-4', CAST(N'2026-09-02T03:28:46.1466667' AS DateTime2), 1, N'Test recepcion contenedor')
INSERT [dbo].[MovimientosInventario] ([IdMovimiento], [IdTemporada], [TipoMovimiento], [IdMaterial], [Cantidad], [IdAlmacenOrigen], [IdAlmacenDestino], [IdEquipoDestino], [IdIglesia], [IdDocumentoReferencia], [FechaHora], [IdUsuario], [Justificacion]) VALUES (9, 1, N'RECEPCION_CONTENEDOR', 3, 160, NULL, 1, NULL, NULL, N'REC-5', CAST(N'2026-09-02T03:29:28.4166667' AS DateTime2), 1, N'Test recepcion contenedor')
INSERT [dbo].[MovimientosInventario] ([IdMovimiento], [IdTemporada], [TipoMovimiento], [IdMaterial], [Cantidad], [IdAlmacenOrigen], [IdAlmacenDestino], [IdEquipoDestino], [IdIglesia], [IdDocumentoReferencia], [FechaHora], [IdUsuario], [Justificacion]) VALUES (10, 1, N'TRANSFERENCIA_EQUIPO', 3, 60, 1, NULL, 1, NULL, N'TRF-4', CAST(N'2026-09-02T03:29:28.4400000' AS DateTime2), 1, N'Test transferencia')
INSERT [dbo].[MovimientosInventario] ([IdMovimiento], [IdTemporada], [TipoMovimiento], [IdMaterial], [Cantidad], [IdAlmacenOrigen], [IdAlmacenDestino], [IdEquipoDestino], [IdIglesia], [IdDocumentoReferencia], [FechaHora], [IdUsuario], [Justificacion]) VALUES (11, 1, N'RECEPCION_CONTENEDOR', 3, 7350, NULL, 2, NULL, NULL, N'REC-6', CAST(N'2026-09-02T15:29:30.6366667' AS DateTime2), 1, N'Recepción contenedor #CONT PRUEBA 001')
INSERT [dbo].[MovimientosInventario] ([IdMovimiento], [IdTemporada], [TipoMovimiento], [IdMaterial], [Cantidad], [IdAlmacenOrigen], [IdAlmacenDestino], [IdEquipoDestino], [IdIglesia], [IdDocumentoReferencia], [FechaHora], [IdUsuario], [Justificacion]) VALUES (12, 1, N'RECEPCION_CONTENEDOR', 3, 7200, NULL, 2, NULL, NULL, N'REC-7', CAST(N'2026-09-02T16:48:56.4333333' AS DateTime2), 1, N'Recepción de contenedor #TONT PRUEBA en almacén ID 2')
INSERT [dbo].[MovimientosInventario] ([IdMovimiento], [IdTemporada], [TipoMovimiento], [IdMaterial], [Cantidad], [IdAlmacenOrigen], [IdAlmacenDestino], [IdEquipoDestino], [IdIglesia], [IdDocumentoReferencia], [FechaHora], [IdUsuario], [Justificacion]) VALUES (13, 1, N'RECEPCION_CONTENEDOR', 3, 7200, NULL, 2, NULL, NULL, N'REC-8', CAST(N'2026-09-02T18:34:50.6566667' AS DateTime2), 1, N'Recepción de contenedor #CONT PRUEBA 050 en almacén ID 2')
INSERT [dbo].[MovimientosInventario] ([IdMovimiento], [IdTemporada], [TipoMovimiento], [IdMaterial], [Cantidad], [IdAlmacenOrigen], [IdAlmacenDestino], [IdEquipoDestino], [IdIglesia], [IdDocumentoReferencia], [FechaHora], [IdUsuario], [Justificacion]) VALUES (14, 1, N'DESPACHO_IGLESIA', 3, 50, NULL, NULL, 24, 3, N'DSP-6', CAST(N'2026-09-02T19:29:54.0266667' AS DateTime2), 1, N'Despacho a iglesia ID 3')
INSERT [dbo].[MovimientosInventario] ([IdMovimiento], [IdTemporada], [TipoMovimiento], [IdMaterial], [Cantidad], [IdAlmacenOrigen], [IdAlmacenDestino], [IdEquipoDestino], [IdIglesia], [IdDocumentoReferencia], [FechaHora], [IdUsuario], [Justificacion]) VALUES (15, 1, N'TRANSFERENCIA_EQUIPO', 3, 7200, 2, NULL, 2, NULL, N'TRF-20260902-6385', CAST(N'2026-09-02T19:46:05.6666667' AS DateTime2), 1, N'Transferencia TRF-20260902-6385 de material ID 3 al equipo ID 2')
INSERT [dbo].[MovimientosInventario] ([IdMovimiento], [IdTemporada], [TipoMovimiento], [IdMaterial], [Cantidad], [IdAlmacenOrigen], [IdAlmacenDestino], [IdEquipoDestino], [IdIglesia], [IdDocumentoReferencia], [FechaHora], [IdUsuario], [Justificacion]) VALUES (16, 1, N'RECEPCION_TRANSFERENCIA', 3, 7200, NULL, NULL, 2, NULL, N'TRF-20260902-6385', CAST(N'2026-09-02T19:46:56.9633333' AS DateTime2), 1, N'Confirmación de recepción física de transferencia TRF-20260902-6385 por el equipo receptor ID 2')
INSERT [dbo].[MovimientosInventario] ([IdMovimiento], [IdTemporada], [TipoMovimiento], [IdMaterial], [Cantidad], [IdAlmacenOrigen], [IdAlmacenDestino], [IdEquipoDestino], [IdIglesia], [IdDocumentoReferencia], [FechaHora], [IdUsuario], [Justificacion]) VALUES (17, 1, N'TRANSFERENCIA_EQUIPO', 3, 7200, 2, NULL, 2, NULL, N'TRF-20260902-2659', CAST(N'2026-09-02T19:55:20.2600000' AS DateTime2), 1, N'Transferencia TRF-20260902-2659 de material ID 3 al equipo ID 2')
INSERT [dbo].[MovimientosInventario] ([IdMovimiento], [IdTemporada], [TipoMovimiento], [IdMaterial], [Cantidad], [IdAlmacenOrigen], [IdAlmacenDestino], [IdEquipoDestino], [IdIglesia], [IdDocumentoReferencia], [FechaHora], [IdUsuario], [Justificacion]) VALUES (18, 1, N'RECEPCION_TRANSFERENCIA', 3, 7200, NULL, NULL, 2, NULL, N'TRF-20260902-2659', CAST(N'2026-09-02T19:55:45.4466667' AS DateTime2), 1, N'Confirmación de recepción física de transferencia TRF-20260902-2659 por el equipo receptor ID 2')
INSERT [dbo].[MovimientosInventario] ([IdMovimiento], [IdTemporada], [TipoMovimiento], [IdMaterial], [Cantidad], [IdAlmacenOrigen], [IdAlmacenDestino], [IdEquipoDestino], [IdIglesia], [IdDocumentoReferencia], [FechaHora], [IdUsuario], [Justificacion]) VALUES (19, 1, N'DESPACHO_IGLESIA', 3, 50, NULL, NULL, 24, 2, N'DSP-5', CAST(N'2026-09-03T04:36:11.4700000' AS DateTime2), 1, N'Despacho a iglesia ID 2')
SET IDENTITY_INSERT [dbo].[MovimientosInventario] OFF
ALTER TABLE [dbo].[MovimientosInventario] ADD  DEFAULT (getdate()) FOR [FechaHora]
ALTER TABLE [dbo].[MovimientosInventario]  WITH CHECK ADD FOREIGN KEY([IdAlmacenOrigen])
REFERENCES [dbo].[Almacenes] ([IdAlmacen])
ALTER TABLE [dbo].[MovimientosInventario]  WITH CHECK ADD FOREIGN KEY([IdAlmacenDestino])
REFERENCES [dbo].[Almacenes] ([IdAlmacen])
ALTER TABLE [dbo].[MovimientosInventario]  WITH CHECK ADD FOREIGN KEY([IdEquipoDestino])
REFERENCES [dbo].[Equipos] ([IdEquipo])
ALTER TABLE [dbo].[MovimientosInventario]  WITH CHECK ADD FOREIGN KEY([IdIglesia])
REFERENCES [dbo].[Iglesias] ([IdIglesia])
ALTER TABLE [dbo].[MovimientosInventario]  WITH CHECK ADD FOREIGN KEY([IdMaterial])
REFERENCES [dbo].[Materiales] ([IdMaterial])
ALTER TABLE [dbo].[MovimientosInventario]  WITH CHECK ADD FOREIGN KEY([IdTemporada])
REFERENCES [dbo].[Temporadas] ([IdTemporada])
ALTER TABLE [dbo].[MovimientosInventario]  WITH CHECK ADD FOREIGN KEY([IdUsuario])
REFERENCES [dbo].[Usuarios] ([IdUsuario])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[NivelesEquipo](
	[IdNivelEquipo] [int] NOT NULL,
	[NombreNivel] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[RangoJerarquico] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdNivelEquipo] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
INSERT [dbo].[NivelesEquipo] ([IdNivelEquipo], [NombreNivel], [RangoJerarquico]) VALUES (1, N'ENL', 1)
INSERT [dbo].[NivelesEquipo] ([IdNivelEquipo], [NombreNivel], [RangoJerarquico]) VALUES (2, N'ERLE', 2)
INSERT [dbo].[NivelesEquipo] ([IdNivelEquipo], [NombreNivel], [RangoJerarquico]) VALUES (3, N'ERL', 3)

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[Notificaciones](
	[IdNotificacion] [int] IDENTITY(1,1) NOT NULL,
	[IdUsuarioDestinatario] [int] NOT NULL,
	[Mensaje] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[FechaCreacion] [datetime] NULL,
	[Leida] [bit] NULL,
	[FechaLectura] [datetime] NULL,
	[IdUsuarioLectura] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdNotificacion] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

ALTER TABLE [dbo].[Notificaciones] ADD  DEFAULT (getdate()) FOR [FechaCreacion]
ALTER TABLE [dbo].[Notificaciones] ADD  DEFAULT ((0)) FOR [Leida]

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[ParticipacionesIglesia](
	[IdParticipacion] [int] IDENTITY(1,1) NOT NULL,
	[IdIglesia] [int] NOT NULL,
	[IdTemporada] [int] NOT NULL,
	[Participara] [bit] NULL,
	[JustificacionNoParticipacion] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[EstadoEvaluacion] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IdUsuarioEvaluador] [int] NULL,
	[FechaSolicitud] [datetime] NULL,
	[FechaEvaluacion] [datetime] NULL,
	[EtapaActual] [int] NULL,
	[EvalInicialEstado] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[EvalInicialMotivo] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[EvalInicialIdUsuario] [int] NULL,
	[EvalInicialFecha] [datetime] NULL,
	[EvalInicialComentario] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[VisionInvitada] [bit] NULL,
	[VisionFecha] [date] NULL,
	[VisionLugar] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[VisionAsistio] [bit] NULL,
	[VisionResultado] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[EvalTallerEstado] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[EvalTallerMotivo] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[EvalTallerIdUsuario] [int] NULL,
	[EvalTallerFecha] [datetime] NULL,
	[EvalTallerComentario] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TallerParticipo] [bit] NULL,
	[TallerNombre] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TallerFecha] [date] NULL,
	[TallerLugar] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TallerCantNinos] [int] NULL,
	[TallerCantMaestrosReg] [int] NULL,
	[TallerCantMaestrosAsist] [int] NULL,
	[TallerCantMaestrosAus] [int] NULL,
	[EstatusEvaluacionReporte] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[IdParticipacion] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
SET IDENTITY_INSERT [dbo].[ParticipacionesIglesia] ON 

INSERT [dbo].[ParticipacionesIglesia] ([IdParticipacion], [IdIglesia], [IdTemporada], [Participara], [JustificacionNoParticipacion], [EstadoEvaluacion], [IdUsuarioEvaluador], [FechaSolicitud], [FechaEvaluacion], [EtapaActual], [EvalInicialEstado], [EvalInicialMotivo], [EvalInicialIdUsuario], [EvalInicialFecha], [EvalInicialComentario], [VisionInvitada], [VisionFecha], [VisionLugar], [VisionAsistio], [VisionResultado], [EvalTallerEstado], [EvalTallerMotivo], [EvalTallerIdUsuario], [EvalTallerFecha], [EvalTallerComentario], [TallerParticipo], [TallerNombre], [TallerFecha], [TallerLugar], [TallerCantNinos], [TallerCantMaestrosReg], [TallerCantMaestrosAsist], [TallerCantMaestrosAus], [EstatusEvaluacionReporte]) VALUES (1, 1, 2, 1, NULL, N'Pendiente', NULL, CAST(N'2026-08-24T22:05:01.693' AS DateTime), NULL, 1, N'Pendiente', NULL, NULL, NULL, NULL, 0, NULL, NULL, 0, NULL, N'Pendiente', NULL, NULL, NULL, NULL, 0, NULL, NULL, NULL, 0, 0, 0, 0, N'Reportó')
INSERT [dbo].[ParticipacionesIglesia] ([IdParticipacion], [IdIglesia], [IdTemporada], [Participara], [JustificacionNoParticipacion], [EstadoEvaluacion], [IdUsuarioEvaluador], [FechaSolicitud], [FechaEvaluacion], [EtapaActual], [EvalInicialEstado], [EvalInicialMotivo], [EvalInicialIdUsuario], [EvalInicialFecha], [EvalInicialComentario], [VisionInvitada], [VisionFecha], [VisionLugar], [VisionAsistio], [VisionResultado], [EvalTallerEstado], [EvalTallerMotivo], [EvalTallerIdUsuario], [EvalTallerFecha], [EvalTallerComentario], [TallerParticipo], [TallerNombre], [TallerFecha], [TallerLugar], [TallerCantNinos], [TallerCantMaestrosReg], [TallerCantMaestrosAsist], [TallerCantMaestrosAus], [EstatusEvaluacionReporte]) VALUES (2, 2, 1, 1, NULL, N'Aprobado', NULL, CAST(N'2026-08-24T22:11:38.407' AS DateTime), NULL, 7, N'Aprobada', NULL, 2, CAST(N'2026-08-24T22:26:37.157' AS DateTime), N'Uncomentario', 0, NULL, NULL, 1, N'Continua', N'Aprobada para Taller OCC', NULL, 3, CAST(N'2026-08-24T23:41:02.610' AS DateTime), N'', 1, N'Despacho Unificado Test 04661c5d', CAST(N'2026-09-02' AS Date), N'Centro Test', 50, 2, 2, 0, N'Pendiente')
INSERT [dbo].[ParticipacionesIglesia] ([IdParticipacion], [IdIglesia], [IdTemporada], [Participara], [JustificacionNoParticipacion], [EstadoEvaluacion], [IdUsuarioEvaluador], [FechaSolicitud], [FechaEvaluacion], [EtapaActual], [EvalInicialEstado], [EvalInicialMotivo], [EvalInicialIdUsuario], [EvalInicialFecha], [EvalInicialComentario], [VisionInvitada], [VisionFecha], [VisionLugar], [VisionAsistio], [VisionResultado], [EvalTallerEstado], [EvalTallerMotivo], [EvalTallerIdUsuario], [EvalTallerFecha], [EvalTallerComentario], [TallerParticipo], [TallerNombre], [TallerFecha], [TallerLugar], [TallerCantNinos], [TallerCantMaestrosReg], [TallerCantMaestrosAsist], [TallerCantMaestrosAus], [EstatusEvaluacionReporte]) VALUES (3, 3, 1, 1, NULL, N'Aprobado', NULL, CAST(N'2026-09-02T16:50:34.237' AS DateTime), NULL, 7, N'Aprobada', NULL, 1, CAST(N'2026-09-02T16:51:38.643' AS DateTime), N'', 0, NULL, NULL, 1, N'Continua', N'Aprobada para Taller OCC', NULL, 1, CAST(N'2026-09-02T17:10:36.050' AS DateTime), N'', 1, N'Taller OCC', CAST(N'2026-09-02' AS Date), N'Sede Central', 50, 1, 1, 0, N'Pendiente')
SET IDENTITY_INSERT [dbo].[ParticipacionesIglesia] OFF
CREATE NONCLUSTERED INDEX [IX_Participaciones_IdTemporada] ON [dbo].[ParticipacionesIglesia]
(
	[IdTemporada] ASC
)
INCLUDE([IdIglesia],[EstadoEvaluacion],[EstatusEvaluacionReporte],[EtapaActual]) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Participaciones_Iglesia_Temporada] ON [dbo].[ParticipacionesIglesia]
(
	[IdIglesia] ASC,
	[IdTemporada] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[ParticipacionesIglesia] ADD  DEFAULT ((1)) FOR [Participara]
ALTER TABLE [dbo].[ParticipacionesIglesia] ADD  DEFAULT ('Pendiente') FOR [EstadoEvaluacion]
ALTER TABLE [dbo].[ParticipacionesIglesia] ADD  DEFAULT (getdate()) FOR [FechaSolicitud]
ALTER TABLE [dbo].[ParticipacionesIglesia] ADD  DEFAULT ((1)) FOR [EtapaActual]
ALTER TABLE [dbo].[ParticipacionesIglesia] ADD  DEFAULT ('Pendiente') FOR [EvalInicialEstado]
ALTER TABLE [dbo].[ParticipacionesIglesia] ADD  DEFAULT ((0)) FOR [VisionInvitada]
ALTER TABLE [dbo].[ParticipacionesIglesia] ADD  DEFAULT ((0)) FOR [VisionAsistio]
ALTER TABLE [dbo].[ParticipacionesIglesia] ADD  DEFAULT ('Pendiente') FOR [EvalTallerEstado]
ALTER TABLE [dbo].[ParticipacionesIglesia] ADD  DEFAULT ((0)) FOR [TallerParticipo]
ALTER TABLE [dbo].[ParticipacionesIglesia] ADD  DEFAULT ((0)) FOR [TallerCantNinos]
ALTER TABLE [dbo].[ParticipacionesIglesia] ADD  DEFAULT ((0)) FOR [TallerCantMaestrosReg]
ALTER TABLE [dbo].[ParticipacionesIglesia] ADD  DEFAULT ((0)) FOR [TallerCantMaestrosAsist]
ALTER TABLE [dbo].[ParticipacionesIglesia] ADD  DEFAULT ((0)) FOR [TallerCantMaestrosAus]
ALTER TABLE [dbo].[ParticipacionesIglesia] ADD  DEFAULT ('Pendiente') FOR [EstatusEvaluacionReporte]
ALTER TABLE [dbo].[ParticipacionesIglesia]  WITH CHECK ADD FOREIGN KEY([EvalInicialIdUsuario])
REFERENCES [dbo].[Usuarios] ([IdUsuario])
ALTER TABLE [dbo].[ParticipacionesIglesia]  WITH CHECK ADD FOREIGN KEY([EvalTallerIdUsuario])
REFERENCES [dbo].[Usuarios] ([IdUsuario])
ALTER TABLE [dbo].[ParticipacionesIglesia]  WITH CHECK ADD FOREIGN KEY([IdIglesia])
REFERENCES [dbo].[Iglesias] ([IdIglesia])
ALTER TABLE [dbo].[ParticipacionesIglesia]  WITH CHECK ADD FOREIGN KEY([IdTemporada])
REFERENCES [dbo].[Temporadas] ([IdTemporada])
ALTER TABLE [dbo].[ParticipacionesIglesia]  WITH CHECK ADD FOREIGN KEY([IdUsuarioEvaluador])
REFERENCES [dbo].[Usuarios] ([IdUsuario])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[PerfilesCoordinador](
	[IdPerfil] [int] IDENTITY(1,1) NOT NULL,
	[IdUsuario] [int] NOT NULL,
	[PrimerNombre] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[OtrosNombres] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PrimerApellido] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[OtrosApellidos] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FechaNacimiento] [date] NULL,
	[Calle] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Numero] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Sector] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Ciudad] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Provincia] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Pais] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Nacionalidad] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Talla] [varchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[NumeroDocumento] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[DocumentoAdjuntoRuta] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[NumeroPasaporte] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PasaporteAdjuntoRuta] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TelefonoFijo] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TelefonoCelularWhatsApp] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Correo] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FotoRuta] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[DatosConyugue] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ContactoEmergencia] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IglesiaLocal] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PastorIglesiaLocal] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CargoIglesiaLocal] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AniosServicioMinisterial] [int] NULL,
	[InfoMinisterial] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[NivelEducativo] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ProfesionCarrera] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[InfoEducativa] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[OcupacionEmpresaLaboral] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TelefonoTrabajo] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[InfoLaboral] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CapacitacionesOCC] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Ministerio] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IdEquipo] [int] NULL,
	[IdPosicion] [int] NULL,
	[FechaIngreso] [date] NULL,
	[FechaCompletado] [datetime] NULL,
	[Sexo] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[EstadoCivil] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[NoPoseePasaporte] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdPerfil] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
SET IDENTITY_INSERT [dbo].[PerfilesCoordinador] ON 

INSERT [dbo].[PerfilesCoordinador] ([IdPerfil], [IdUsuario], [PrimerNombre], [OtrosNombres], [PrimerApellido], [OtrosApellidos], [FechaNacimiento], [Calle], [Numero], [Sector], [Ciudad], [Provincia], [Pais], [Nacionalidad], [Talla], [NumeroDocumento], [DocumentoAdjuntoRuta], [NumeroPasaporte], [PasaporteAdjuntoRuta], [TelefonoFijo], [TelefonoCelularWhatsApp], [Correo], [FotoRuta], [DatosConyugue], [ContactoEmergencia], [IglesiaLocal], [PastorIglesiaLocal], [CargoIglesiaLocal], [AniosServicioMinisterial], [InfoMinisterial], [NivelEducativo], [ProfesionCarrera], [InfoEducativa], [OcupacionEmpresaLaboral], [TelefonoTrabajo], [InfoLaboral], [CapacitacionesOCC], [Ministerio], [IdEquipo], [IdPosicion], [FechaIngreso], [FechaCompletado], [Sexo], [EstadoCivil], [NoPoseePasaporte]) VALUES (1, 1, N'Juan', N'Esteban', N'Astacio', N'Tejeda', CAST(N'1995-07-22' AS Date), N'10', N'15', N'Los peralejos', N'Santo Domingo', NULL, N'República Dominicana', N'Dominicana', N'XL', N'40224442406', NULL, NULL, NULL, NULL, N'809-555-0000', N'admin@occrd.org', NULL, NULL, NULL, N'Iglesia Central OCC', N'Pastor Principal', N'Líder de Red', NULL, NULL, N'Licenciatura', N'Administración', NULL, NULL, NULL, NULL, NULL, NULL, 24, 3, CAST(N'2012-07-22' AS Date), CAST(N'2026-08-24T13:58:56.027' AS DateTime), N'Varón', N'Casado', 1)
INSERT [dbo].[PerfilesCoordinador] ([IdPerfil], [IdUsuario], [PrimerNombre], [OtrosNombres], [PrimerApellido], [OtrosApellidos], [FechaNacimiento], [Calle], [Numero], [Sector], [Ciudad], [Provincia], [Pais], [Nacionalidad], [Talla], [NumeroDocumento], [DocumentoAdjuntoRuta], [NumeroPasaporte], [PasaporteAdjuntoRuta], [TelefonoFijo], [TelefonoCelularWhatsApp], [Correo], [FotoRuta], [DatosConyugue], [ContactoEmergencia], [IglesiaLocal], [PastorIglesiaLocal], [CargoIglesiaLocal], [AniosServicioMinisterial], [InfoMinisterial], [NivelEducativo], [ProfesionCarrera], [InfoEducativa], [OcupacionEmpresaLaboral], [TelefonoTrabajo], [InfoLaboral], [CapacitacionesOCC], [Ministerio], [IdEquipo], [IdPosicion], [FechaIngreso], [FechaCompletado], [Sexo], [EstadoCivil], [NoPoseePasaporte]) VALUES (10, 2, N'CMI', NULL, N'Rol', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'República Dominicana', N'Dominicana', NULL, NULL, NULL, NULL, NULL, NULL, N'8295323518', N'CMI@correo.com', NULL, NULL, NULL, N'Iglesia Local', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 2, 2, CAST(N'2026-08-24' AS Date), CAST(N'2026-08-24T22:14:49.783' AS DateTime), NULL, NULL, 0)
INSERT [dbo].[PerfilesCoordinador] ([IdPerfil], [IdUsuario], [PrimerNombre], [OtrosNombres], [PrimerApellido], [OtrosApellidos], [FechaNacimiento], [Calle], [Numero], [Sector], [Ciudad], [Provincia], [Pais], [Nacionalidad], [Talla], [NumeroDocumento], [DocumentoAdjuntoRuta], [NumeroPasaporte], [PasaporteAdjuntoRuta], [TelefonoFijo], [TelefonoCelularWhatsApp], [Correo], [FotoRuta], [DatosConyugue], [ContactoEmergencia], [IglesiaLocal], [PastorIglesiaLocal], [CargoIglesiaLocal], [AniosServicioMinisterial], [InfoMinisterial], [NivelEducativo], [ProfesionCarrera], [InfoEducativa], [OcupacionEmpresaLaboral], [TelefonoTrabajo], [InfoLaboral], [CapacitacionesOCC], [Ministerio], [IdEquipo], [IdPosicion], [FechaIngreso], [FechaCompletado], [Sexo], [EstadoCivil], [NoPoseePasaporte]) VALUES (11, 3, N'CD', NULL, N'Rol', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'República Dominicana', N'Dominicana', NULL, NULL, NULL, NULL, NULL, NULL, N'8295323518', N'cd@correo.com', NULL, NULL, NULL, N'Iglesia Local', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 2, 3, NULL, CAST(N'2026-08-24T22:15:30.810' AS DateTime), NULL, NULL, 0)
INSERT [dbo].[PerfilesCoordinador] ([IdPerfil], [IdUsuario], [PrimerNombre], [OtrosNombres], [PrimerApellido], [OtrosApellidos], [FechaNacimiento], [Calle], [Numero], [Sector], [Ciudad], [Provincia], [Pais], [Nacionalidad], [Talla], [NumeroDocumento], [DocumentoAdjuntoRuta], [NumeroPasaporte], [PasaporteAdjuntoRuta], [TelefonoFijo], [TelefonoCelularWhatsApp], [Correo], [FotoRuta], [DatosConyugue], [ContactoEmergencia], [IglesiaLocal], [PastorIglesiaLocal], [CargoIglesiaLocal], [AniosServicioMinisterial], [InfoMinisterial], [NivelEducativo], [ProfesionCarrera], [InfoEducativa], [OcupacionEmpresaLaboral], [TelefonoTrabajo], [InfoLaboral], [CapacitacionesOCC], [Ministerio], [IdEquipo], [IdPosicion], [FechaIngreso], [FechaCompletado], [Sexo], [EstadoCivil], [NoPoseePasaporte]) VALUES (12, 4, N'Efesos', N'Juan', N'Astacio', NULL, NULL, NULL, NULL, NULL, N'Santo Domingo', NULL, N'República Dominicana', N'Dominicana', NULL, NULL, NULL, NULL, NULL, NULL, N'8295323518', N'cm@erle.com', NULL, NULL, NULL, N'Eventos Kairos', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 24, 1, CAST(N'2026-09-02' AS Date), CAST(N'2026-09-02T01:21:39.330' AS DateTime), NULL, NULL, 0)
SET IDENTITY_INSERT [dbo].[PerfilesCoordinador] OFF
ALTER TABLE [dbo].[PerfilesCoordinador] ADD  DEFAULT ('República Dominicana') FOR [Pais]
ALTER TABLE [dbo].[PerfilesCoordinador] ADD  DEFAULT ('Dominicana') FOR [Nacionalidad]
ALTER TABLE [dbo].[PerfilesCoordinador] ADD  DEFAULT (getdate()) FOR [FechaCompletado]
ALTER TABLE [dbo].[PerfilesCoordinador] ADD  DEFAULT ((0)) FOR [NoPoseePasaporte]
ALTER TABLE [dbo].[PerfilesCoordinador]  WITH CHECK ADD FOREIGN KEY([IdEquipo])
REFERENCES [dbo].[Equipos] ([IdEquipo])
ALTER TABLE [dbo].[PerfilesCoordinador]  WITH CHECK ADD FOREIGN KEY([IdPosicion])
REFERENCES [dbo].[PosicionesOCC] ([IdPosicion])
ALTER TABLE [dbo].[PerfilesCoordinador]  WITH CHECK ADD FOREIGN KEY([IdUsuario])
REFERENCES [dbo].[Usuarios] ([IdUsuario])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[PersonasIglesia](
	[IdPersonaIglesia] [int] IDENTITY(1,1) NOT NULL,
	[IdIglesia] [int] NOT NULL,
	[TipoPersona] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Nombres] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Apellidos] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[DocumentoIdentidad] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[DocumentoAdjuntoRuta] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Celular] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Correo] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Calle] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Numero] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Sector] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Referencia] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[IdPersonaIglesia] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
SET IDENTITY_INSERT [dbo].[PersonasIglesia] ON 

INSERT [dbo].[PersonasIglesia] ([IdPersonaIglesia], [IdIglesia], [TipoPersona], [Nombres], [Apellidos], [DocumentoIdentidad], [DocumentoAdjuntoRuta], [Celular], [Correo], [Calle], [Numero], [Sector], [Referencia]) VALUES (1, 1, N'Pastor', N'Juan', N'Perez', N'741852963', NULL, N'8092323518', N'pastor@ejemplo.com', NULL, NULL, NULL, NULL)
INSERT [dbo].[PersonasIglesia] ([IdPersonaIglesia], [IdIglesia], [TipoPersona], [Nombres], [Apellidos], [DocumentoIdentidad], [DocumentoAdjuntoRuta], [Celular], [Correo], [Calle], [Numero], [Sector], [Referencia]) VALUES (2, 1, N'LiderMinisterial', N'Maria', N'Gomez', N'741852963', NULL, N'8094567485', N'lider@ejemplo.com', NULL, NULL, NULL, NULL)
INSERT [dbo].[PersonasIglesia] ([IdPersonaIglesia], [IdIglesia], [TipoPersona], [Nombres], [Apellidos], [DocumentoIdentidad], [DocumentoAdjuntoRuta], [Celular], [Correo], [Calle], [Numero], [Sector], [Referencia]) VALUES (3, 2, N'Pastor', N'Pedro', N'Picaso', N'40224472406', NULL, N'8295656565', N'erlegsd.occrd@gmail.com', NULL, NULL, NULL, NULL)
INSERT [dbo].[PersonasIglesia] ([IdPersonaIglesia], [IdIglesia], [TipoPersona], [Nombres], [Apellidos], [DocumentoIdentidad], [DocumentoAdjuntoRuta], [Celular], [Correo], [Calle], [Numero], [Sector], [Referencia]) VALUES (4, 2, N'LiderMinisterial', N'Maria', N'Parlanchina', N'40258789654', NULL, N'8497898523', N'erlegsd.occrd@gmail.com', NULL, NULL, NULL, NULL)
INSERT [dbo].[PersonasIglesia] ([IdPersonaIglesia], [IdIglesia], [TipoPersona], [Nombres], [Apellidos], [DocumentoIdentidad], [DocumentoAdjuntoRuta], [Celular], [Correo], [Calle], [Numero], [Sector], [Referencia]) VALUES (5, 3, N'Pastor', N'Servicio', N'Al Cliente', N'40224442406', NULL, N'8295656565', N'portaforza@gmail.com', NULL, NULL, NULL, NULL)
INSERT [dbo].[PersonasIglesia] ([IdPersonaIglesia], [IdIglesia], [TipoPersona], [Nombres], [Apellidos], [DocumentoIdentidad], [DocumentoAdjuntoRuta], [Celular], [Correo], [Calle], [Numero], [Sector], [Referencia]) VALUES (6, 3, N'LiderMinisterial', N'Servicio', N'Al Cliente', N'40145856587', NULL, N'8497898523', N'portaforza@gmail.com', NULL, NULL, NULL, NULL)
SET IDENTITY_INSERT [dbo].[PersonasIglesia] OFF
SET ANSI_PADDING ON

CREATE NONCLUSTERED INDEX [IX_PersonasIglesia_IdIglesia_Tipo] ON [dbo].[PersonasIglesia]
(
	[IdIglesia] ASC,
	[TipoPersona] ASC
)
INCLUDE([Nombres],[Apellidos],[DocumentoIdentidad],[Celular],[Correo]) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[PersonasIglesia]  WITH CHECK ADD FOREIGN KEY([IdIglesia])
REFERENCES [dbo].[Iglesias] ([IdIglesia])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[PosicionesOCC](
	[IdPosicion] [int] NOT NULL,
	[NombrePosicion] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Descripcion] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[IdPosicion] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
INSERT [dbo].[PosicionesOCC] ([IdPosicion], [NombrePosicion], [Descripcion]) VALUES (1, N'Coordinador de Equipo', N'Líder principal del equipo')
INSERT [dbo].[PosicionesOCC] ([IdPosicion], [NombrePosicion], [Descripcion]) VALUES (2, N'Coordinador de Movilización', N'Encargado de movilización e iglesias')
INSERT [dbo].[PosicionesOCC] ([IdPosicion], [NombrePosicion], [Descripcion]) VALUES (3, N'Coordinador de Discipulado', N'Encargado de discipulado y capacitaciones')
INSERT [dbo].[PosicionesOCC] ([IdPosicion], [NombrePosicion], [Descripcion]) VALUES (4, N'Coordinador de Recursos', N'Encargado de inventarios y recursos')
INSERT [dbo].[PosicionesOCC] ([IdPosicion], [NombrePosicion], [Descripcion]) VALUES (5, N'Coordinador de Oración', N'Encargado de la red de oración')
INSERT [dbo].[PosicionesOCC] ([IdPosicion], [NombrePosicion], [Descripcion]) VALUES (6, N'Coordinador de Logística', N'Encargado de despachos y logística')

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[PresentacionesMaterial](
	[IdPresentacion] [int] IDENTITY(1,1) NOT NULL,
	[IdMaterial] [int] NOT NULL,
	[TipoEmpaque] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[UnidadesPorEmpaque] [int] NOT NULL,
	[IdTemporadaVigencia] [int] NULL,
	[FechaVigenciaInicio] [datetime2](7) NOT NULL,
	[Activo] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdPresentacion] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
SET IDENTITY_INSERT [dbo].[PresentacionesMaterial] ON 

INSERT [dbo].[PresentacionesMaterial] ([IdPresentacion], [IdMaterial], [TipoEmpaque], [UnidadesPorEmpaque], [IdTemporadaVigencia], [FechaVigenciaInicio], [Activo]) VALUES (1, 7, N'Caja', 100, NULL, CAST(N'2026-09-02T02:48:06.3333333' AS DateTime2), 1)
INSERT [dbo].[PresentacionesMaterial] ([IdPresentacion], [IdMaterial], [TipoEmpaque], [UnidadesPorEmpaque], [IdTemporadaVigencia], [FechaVigenciaInicio], [Activo]) VALUES (2, 2, N'Caja', 45, NULL, CAST(N'2026-09-02T02:48:06.3333333' AS DateTime2), 1)
INSERT [dbo].[PresentacionesMaterial] ([IdPresentacion], [IdMaterial], [TipoEmpaque], [UnidadesPorEmpaque], [IdTemporadaVigencia], [FechaVigenciaInicio], [Activo]) VALUES (3, 1, N'Caja', 20, NULL, CAST(N'2026-09-02T02:48:06.3333333' AS DateTime2), 1)
INSERT [dbo].[PresentacionesMaterial] ([IdPresentacion], [IdMaterial], [TipoEmpaque], [UnidadesPorEmpaque], [IdTemporadaVigencia], [FechaVigenciaInicio], [Activo]) VALUES (4, 4, N'Caja', 200, NULL, CAST(N'2026-09-02T02:48:06.3333333' AS DateTime2), 1)
INSERT [dbo].[PresentacionesMaterial] ([IdPresentacion], [IdMaterial], [TipoEmpaque], [UnidadesPorEmpaque], [IdTemporadaVigencia], [FechaVigenciaInicio], [Activo]) VALUES (5, 6, N'Caja', 30, NULL, CAST(N'2026-09-02T02:48:06.3333333' AS DateTime2), 1)
INSERT [dbo].[PresentacionesMaterial] ([IdPresentacion], [IdMaterial], [TipoEmpaque], [UnidadesPorEmpaque], [IdTemporadaVigencia], [FechaVigenciaInicio], [Activo]) VALUES (6, 3, N'Caja', 16, NULL, CAST(N'2026-09-02T02:48:06.3333333' AS DateTime2), 0)
INSERT [dbo].[PresentacionesMaterial] ([IdPresentacion], [IdMaterial], [TipoEmpaque], [UnidadesPorEmpaque], [IdTemporadaVigencia], [FechaVigenciaInicio], [Activo]) VALUES (7, 5, N'Caja', 25, NULL, CAST(N'2026-09-02T02:48:06.3333333' AS DateTime2), 1)
INSERT [dbo].[PresentacionesMaterial] ([IdPresentacion], [IdMaterial], [TipoEmpaque], [UnidadesPorEmpaque], [IdTemporadaVigencia], [FechaVigenciaInicio], [Activo]) VALUES (8, 3, N'Caja', 16, 1, CAST(N'2026-09-02T02:48:06.3366667' AS DateTime2), 0)
INSERT [dbo].[PresentacionesMaterial] ([IdPresentacion], [IdMaterial], [TipoEmpaque], [UnidadesPorEmpaque], [IdTemporadaVigencia], [FechaVigenciaInicio], [Activo]) VALUES (9, 3, N'Caja', 15, 1, CAST(N'2026-09-02T13:43:51.3700000' AS DateTime2), 1)
SET IDENTITY_INSERT [dbo].[PresentacionesMaterial] OFF
ALTER TABLE [dbo].[PresentacionesMaterial] ADD  DEFAULT (getdate()) FOR [FechaVigenciaInicio]
ALTER TABLE [dbo].[PresentacionesMaterial] ADD  DEFAULT ((1)) FOR [Activo]
ALTER TABLE [dbo].[PresentacionesMaterial]  WITH CHECK ADD FOREIGN KEY([IdMaterial])
REFERENCES [dbo].[Materiales] ([IdMaterial])
ALTER TABLE [dbo].[PresentacionesMaterial]  WITH CHECK ADD FOREIGN KEY([IdTemporadaVigencia])
REFERENCES [dbo].[Temporadas] ([IdTemporada])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[RecepcionesContenedor](
	[IdRecepcion] [int] IDENTITY(1,1) NOT NULL,
	[NumeroContenedor] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[IdTemporada] [int] NOT NULL,
	[IdAlmacen] [int] NOT NULL,
	[FechaRecepcion] [datetime2](7) NOT NULL,
	[ResponsableRecepcion] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Observaciones] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[EstadoRecepcion] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[IdUsuarioRegistro] [int] NOT NULL,
	[FechaRegistro] [datetime2](7) NOT NULL,
	[HoraRecepcion] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IdEquipoReceptor] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdRecepcion] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
SET IDENTITY_INSERT [dbo].[RecepcionesContenedor] ON 

INSERT [dbo].[RecepcionesContenedor] ([IdRecepcion], [NumeroContenedor], [IdTemporada], [IdAlmacen], [FechaRecepcion], [ResponsableRecepcion], [Observaciones], [EstadoRecepcion], [IdUsuarioRegistro], [FechaRegistro], [HoraRecepcion], [IdEquipoReceptor]) VALUES (1, N'TEST-CONT-77110', 1, 1, CAST(N'2026-09-02T02:54:13.1600000' AS DateTime2), N'Encargado Pruebas', NULL, N'CONFIRMADA', 1, CAST(N'2026-09-02T02:54:13.1600000' AS DateTime2), NULL, NULL)
INSERT [dbo].[RecepcionesContenedor] ([IdRecepcion], [NumeroContenedor], [IdTemporada], [IdAlmacen], [FechaRecepcion], [ResponsableRecepcion], [Observaciones], [EstadoRecepcion], [IdUsuarioRegistro], [FechaRegistro], [HoraRecepcion], [IdEquipoReceptor]) VALUES (2, N'TEST-CONT-33695', 1, 1, CAST(N'2026-09-02T02:54:50.9666667' AS DateTime2), N'Encargado Pruebas', NULL, N'CONFIRMADA', 1, CAST(N'2026-09-02T02:54:50.9666667' AS DateTime2), NULL, NULL)
INSERT [dbo].[RecepcionesContenedor] ([IdRecepcion], [NumeroContenedor], [IdTemporada], [IdAlmacen], [FechaRecepcion], [ResponsableRecepcion], [Observaciones], [EstadoRecepcion], [IdUsuarioRegistro], [FechaRegistro], [HoraRecepcion], [IdEquipoReceptor]) VALUES (3, N'TEST-CONT-69590', 1, 1, CAST(N'2026-09-02T02:55:11.8333333' AS DateTime2), N'Encargado Pruebas', NULL, N'CONFIRMADA', 1, CAST(N'2026-09-02T02:55:11.8333333' AS DateTime2), NULL, NULL)
INSERT [dbo].[RecepcionesContenedor] ([IdRecepcion], [NumeroContenedor], [IdTemporada], [IdAlmacen], [FechaRecepcion], [ResponsableRecepcion], [Observaciones], [EstadoRecepcion], [IdUsuarioRegistro], [FechaRegistro], [HoraRecepcion], [IdEquipoReceptor]) VALUES (4, N'TEST-CONT-3455', 1, 1, CAST(N'2026-09-02T03:28:46.1266667' AS DateTime2), N'Test Runner', NULL, N'CONFIRMADA', 1, CAST(N'2026-09-02T03:28:46.1266667' AS DateTime2), NULL, NULL)
INSERT [dbo].[RecepcionesContenedor] ([IdRecepcion], [NumeroContenedor], [IdTemporada], [IdAlmacen], [FechaRecepcion], [ResponsableRecepcion], [Observaciones], [EstadoRecepcion], [IdUsuarioRegistro], [FechaRegistro], [HoraRecepcion], [IdEquipoReceptor]) VALUES (5, N'TEST-CONT-8575', 1, 1, CAST(N'2026-09-02T03:29:28.4000000' AS DateTime2), N'Test Runner', NULL, N'CONFIRMADA', 1, CAST(N'2026-09-02T03:29:28.4000000' AS DateTime2), NULL, NULL)
INSERT [dbo].[RecepcionesContenedor] ([IdRecepcion], [NumeroContenedor], [IdTemporada], [IdAlmacen], [FechaRecepcion], [ResponsableRecepcion], [Observaciones], [EstadoRecepcion], [IdUsuarioRegistro], [FechaRegistro], [HoraRecepcion], [IdEquipoReceptor]) VALUES (6, N'CONT PRUEBA 001', 1, 2, CAST(N'2026-09-02T00:00:00.0000000' AS DateTime2), N'Efesos Astacio', N'', N'CONFIRMADA', 1, CAST(N'2026-09-02T15:29:30.6166667' AS DateTime2), NULL, NULL)
INSERT [dbo].[RecepcionesContenedor] ([IdRecepcion], [NumeroContenedor], [IdTemporada], [IdAlmacen], [FechaRecepcion], [ResponsableRecepcion], [Observaciones], [EstadoRecepcion], [IdUsuarioRegistro], [FechaRegistro], [HoraRecepcion], [IdEquipoReceptor]) VALUES (7, N'TONT PRUEBA', 1, 2, CAST(N'2026-09-02T00:00:00.0000000' AS DateTime2), N'Efesos Astacio (Coordinador de Equipo - ÉRLE Nuevo)', N'', N'CONFIRMADA', 1, CAST(N'2026-09-02T16:48:56.4200000' AS DateTime2), N'04:47 p. m.', 24)
INSERT [dbo].[RecepcionesContenedor] ([IdRecepcion], [NumeroContenedor], [IdTemporada], [IdAlmacen], [FechaRecepcion], [ResponsableRecepcion], [Observaciones], [EstadoRecepcion], [IdUsuarioRegistro], [FechaRegistro], [HoraRecepcion], [IdEquipoReceptor]) VALUES (8, N'CONT PRUEBA 050', 1, 2, CAST(N'2026-09-02T00:00:00.0000000' AS DateTime2), N'Efesos Astacio (Coordinador de Equipo - ÉRLE Nuevo)', N'', N'CONFIRMADA', 1, CAST(N'2026-09-02T18:34:50.6366667' AS DateTime2), N'06:33 p. m.', 24)
SET IDENTITY_INSERT [dbo].[RecepcionesContenedor] OFF
ALTER TABLE [dbo].[RecepcionesContenedor] ADD  DEFAULT (getdate()) FOR [FechaRecepcion]
ALTER TABLE [dbo].[RecepcionesContenedor] ADD  DEFAULT ('CONFIRMADA') FOR [EstadoRecepcion]
ALTER TABLE [dbo].[RecepcionesContenedor] ADD  DEFAULT (getdate()) FOR [FechaRegistro]
ALTER TABLE [dbo].[RecepcionesContenedor]  WITH CHECK ADD FOREIGN KEY([IdAlmacen])
REFERENCES [dbo].[Almacenes] ([IdAlmacen])
ALTER TABLE [dbo].[RecepcionesContenedor]  WITH CHECK ADD FOREIGN KEY([IdEquipoReceptor])
REFERENCES [dbo].[Equipos] ([IdEquipo])
ALTER TABLE [dbo].[RecepcionesContenedor]  WITH CHECK ADD FOREIGN KEY([IdTemporada])
REFERENCES [dbo].[Temporadas] ([IdTemporada])
ALTER TABLE [dbo].[RecepcionesContenedor]  WITH CHECK ADD FOREIGN KEY([IdUsuarioRegistro])
REFERENCES [dbo].[Usuarios] ([IdUsuario])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[RecepcionesContenedorDetalle](
	[IdRecepcionDetalle] [int] IDENTITY(1,1) NOT NULL,
	[IdRecepcion] [int] NOT NULL,
	[IdMaterial] [int] NOT NULL,
	[IdPresentacion] [int] NULL,
	[CantidadEmpaques] [int] NOT NULL,
	[UnidadesPorEmpaque] [int] NOT NULL,
	[CantidadTotalUnidades]  AS ([CantidadEmpaques]*[UnidadesPorEmpaque]) PERSISTED,
PRIMARY KEY CLUSTERED 
(
	[IdRecepcionDetalle] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
SET ANSI_PADDING ON
SET IDENTITY_INSERT [dbo].[RecepcionesContenedorDetalle] ON 

INSERT [dbo].[RecepcionesContenedorDetalle] ([IdRecepcionDetalle], [IdRecepcion], [IdMaterial], [IdPresentacion], [CantidadEmpaques], [UnidadesPorEmpaque]) VALUES (1, 1, 1, NULL, 10, 20)
INSERT [dbo].[RecepcionesContenedorDetalle] ([IdRecepcionDetalle], [IdRecepcion], [IdMaterial], [IdPresentacion], [CantidadEmpaques], [UnidadesPorEmpaque]) VALUES (2, 2, 1, NULL, 10, 20)
INSERT [dbo].[RecepcionesContenedorDetalle] ([IdRecepcionDetalle], [IdRecepcion], [IdMaterial], [IdPresentacion], [CantidadEmpaques], [UnidadesPorEmpaque]) VALUES (3, 3, 1, NULL, 10, 20)
INSERT [dbo].[RecepcionesContenedorDetalle] ([IdRecepcionDetalle], [IdRecepcion], [IdMaterial], [IdPresentacion], [CantidadEmpaques], [UnidadesPorEmpaque]) VALUES (5, 5, 3, 6, 10, 16)
INSERT [dbo].[RecepcionesContenedorDetalle] ([IdRecepcionDetalle], [IdRecepcion], [IdMaterial], [IdPresentacion], [CantidadEmpaques], [UnidadesPorEmpaque]) VALUES (6, 6, 3, 9, 490, 15)
INSERT [dbo].[RecepcionesContenedorDetalle] ([IdRecepcionDetalle], [IdRecepcion], [IdMaterial], [IdPresentacion], [CantidadEmpaques], [UnidadesPorEmpaque]) VALUES (7, 7, 3, 9, 480, 15)
INSERT [dbo].[RecepcionesContenedorDetalle] ([IdRecepcionDetalle], [IdRecepcion], [IdMaterial], [IdPresentacion], [CantidadEmpaques], [UnidadesPorEmpaque]) VALUES (8, 8, 3, 9, 480, 15)
SET IDENTITY_INSERT [dbo].[RecepcionesContenedorDetalle] OFF
SET ANSI_PADDING OFF
ALTER TABLE [dbo].[RecepcionesContenedorDetalle]  WITH CHECK ADD FOREIGN KEY([IdMaterial])
REFERENCES [dbo].[Materiales] ([IdMaterial])
ALTER TABLE [dbo].[RecepcionesContenedorDetalle]  WITH CHECK ADD FOREIGN KEY([IdPresentacion])
REFERENCES [dbo].[PresentacionesMaterial] ([IdPresentacion])
ALTER TABLE [dbo].[RecepcionesContenedorDetalle]  WITH CHECK ADD FOREIGN KEY([IdRecepcion])
REFERENCES [dbo].[RecepcionesContenedor] ([IdRecepcion])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[ReportesEventos](
	[IdReporteEvento] [int] IDENTITY(1,1) NOT NULL,
	[IdParticipacion] [int] NOT NULL,
	[TipoReporte] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Fecha] [date] NULL,
	[CantidadNinos] [int] NULL,
	[CantidadClases] [int] NULL,
	[AsistenciaPorClase] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CuantosAceptaronSenor] [int] NULL,
	[CuantosComprometieron] [int] NULL,
	[CuantosGraduaron] [int] NULL,
	[ReporteAdjuntoRuta] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Notas] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FechaCreacion] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdReporteEvento] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[ReportesEventos] ADD  DEFAULT ((0)) FOR [CantidadNinos]
ALTER TABLE [dbo].[ReportesEventos] ADD  DEFAULT ((0)) FOR [CantidadClases]
ALTER TABLE [dbo].[ReportesEventos] ADD  DEFAULT ((0)) FOR [CuantosAceptaronSenor]
ALTER TABLE [dbo].[ReportesEventos] ADD  DEFAULT ((0)) FOR [CuantosComprometieron]
ALTER TABLE [dbo].[ReportesEventos] ADD  DEFAULT ((0)) FOR [CuantosGraduaron]
ALTER TABLE [dbo].[ReportesEventos] ADD  DEFAULT (getdate()) FOR [FechaCreacion]
ALTER TABLE [dbo].[ReportesEventos]  WITH CHECK ADD FOREIGN KEY([IdParticipacion])
REFERENCES [dbo].[ParticipacionesIglesia] ([IdParticipacion])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[RolesEvento](
	[IdRolEvento] [int] IDENTITY(1,1) NOT NULL,
	[Nombre] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Descripcion] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Activo] [bit] NOT NULL,
	[FechaCreacion] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdRolEvento] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET IDENTITY_INSERT [dbo].[RolesEvento] ON 

INSERT [dbo].[RolesEvento] ([IdRolEvento], [Nombre], [Descripcion], [Activo], [FechaCreacion]) VALUES (1, N'Coordinador Principal / Encargado', N'Responsable general de la conducción del evento', 1, CAST(N'2026-09-03T04:48:23.657' AS DateTime))
INSERT [dbo].[RolesEvento] ([IdRolEvento], [Nombre], [Descripcion], [Activo], [FechaCreacion]) VALUES (2, N'Facilitador / Expositor', N'Imparte el contenido, dinámicas o presentaciones del evento', 1, CAST(N'2026-09-03T04:48:23.657' AS DateTime))
INSERT [dbo].[RolesEvento] ([IdRolEvento], [Nombre], [Descripcion], [Activo], [FechaCreacion]) VALUES (3, N'Logística y Despacho', N'Coordinación de paquetes, materiales y suministros', 1, CAST(N'2026-09-03T04:48:23.657' AS DateTime))
INSERT [dbo].[RolesEvento] ([IdRolEvento], [Nombre], [Descripcion], [Activo], [FechaCreacion]) VALUES (4, N'Registro y Asistencia', N'Mesa de recepción, validación de cédulas y asistencia', 1, CAST(N'2026-09-03T04:48:23.657' AS DateTime))
INSERT [dbo].[RolesEvento] ([IdRolEvento], [Nombre], [Descripcion], [Activo], [FechaCreacion]) VALUES (5, N'Acompañamiento y Bienvenida', N'Atención personalizada a pastores y líderes asistentes', 1, CAST(N'2026-09-03T04:48:23.657' AS DateTime))
INSERT [dbo].[RolesEvento] ([IdRolEvento], [Nombre], [Descripcion], [Activo], [FechaCreacion]) VALUES (6, N'Intercesión y Oración', N'Cobertura espiritual y oración durante el desarrollo del evento', 1, CAST(N'2026-09-03T04:48:23.657' AS DateTime))
INSERT [dbo].[RolesEvento] ([IdRolEvento], [Nombre], [Descripcion], [Activo], [FechaCreacion]) VALUES (7, N'Apoyo General', N'Soporte y asistencia operativa en diversas áreas', 1, CAST(N'2026-09-03T04:48:23.657' AS DateTime))
SET IDENTITY_INSERT [dbo].[RolesEvento] OFF
ALTER TABLE [dbo].[RolesEvento] ADD  DEFAULT ((1)) FOR [Activo]
ALTER TABLE [dbo].[RolesEvento] ADD  DEFAULT (getdate()) FOR [FechaCreacion]

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[RolesSeguridad](
	[IdRolSeguridad] [int] NOT NULL,
	[NombreRol] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Descripcion] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[IdRolSeguridad] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
INSERT [dbo].[RolesSeguridad] ([IdRolSeguridad], [NombreRol], [Descripcion]) VALUES (1, N'SuperAdmin', N'Super Administrador con permisos totales y asignación de admins')
INSERT [dbo].[RolesSeguridad] ([IdRolSeguridad], [NombreRol], [Descripcion]) VALUES (2, N'Administrador', N'Administrador del sistema')
INSERT [dbo].[RolesSeguridad] ([IdRolSeguridad], [NombreRol], [Descripcion]) VALUES (3, N'Coordinador', N'Coordinador estándar (rol por defecto)')

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[SeguimientoLGAContactos](
	[IdSeguimiento] [int] IDENTITY(1,1) NOT NULL,
	[IdParticipacion] [int] NOT NULL,
	[IdIglesia] [int] NOT NULL,
	[NumeroContacto] [int] NOT NULL,
	[FechaContacto] [datetime] NULL,
	[IdUsuarioContacto] [int] NULL,
	[PreguntaClave] [nvarchar](250) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[DatoMinimo1] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[DatoMinimo2] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[DatoMinimo3] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[DecisionTomada] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ComentarioAccion] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[EstadoContacto] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[FechaRegistro] [datetime] NOT NULL,
	[FechaModificacion] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdSeguimiento] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

ALTER TABLE [dbo].[SeguimientoLGAContactos] ADD  CONSTRAINT [UQ_SeguimientoLGA_Part_Num] UNIQUE NONCLUSTERED 
(
	[IdParticipacion] ASC,
	[NumeroContacto] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_SeguimientoLGA_Iglesia] ON [dbo].[SeguimientoLGAContactos]
(
	[IdIglesia] ASC,
	[NumeroContacto] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[SeguimientoLGAContactos] ADD  DEFAULT ('PENDIENTE') FOR [EstadoContacto]
ALTER TABLE [dbo].[SeguimientoLGAContactos] ADD  DEFAULT (getdate()) FOR [FechaRegistro]

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Temporadas](
	[IdTemporada] [int] IDENTITY(1,1) NOT NULL,
	[NombreTemporada] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[FechaInicio] [date] NULL,
	[FechaFin] [date] NULL,
	[Activa] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdTemporada] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
SET IDENTITY_INSERT [dbo].[Temporadas] ON 

INSERT [dbo].[Temporadas] ([IdTemporada], [NombreTemporada], [FechaInicio], [FechaFin], [Activa]) VALUES (1, N'Temp 2026-2027', CAST(N'2026-08-01' AS Date), CAST(N'2027-08-31' AS Date), 1)
INSERT [dbo].[Temporadas] ([IdTemporada], [NombreTemporada], [FechaInicio], [FechaFin], [Activa]) VALUES (2, N'Temp 2025-2026', CAST(N'2025-09-01' AS Date), CAST(N'2026-07-31' AS Date), 0)
INSERT [dbo].[Temporadas] ([IdTemporada], [NombreTemporada], [FechaInicio], [FechaFin], [Activa]) VALUES (3, N'Temp 2024-2025', CAST(N'2024-09-01' AS Date), CAST(N'2025-08-31' AS Date), 0)
INSERT [dbo].[Temporadas] ([IdTemporada], [NombreTemporada], [FechaInicio], [FechaFin], [Activa]) VALUES (4, N'Temp 2023-2024', CAST(N'2023-09-01' AS Date), CAST(N'2024-08-31' AS Date), 0)
SET IDENTITY_INSERT [dbo].[Temporadas] OFF
ALTER TABLE [dbo].[Temporadas] ADD  DEFAULT ((1)) FOR [Activa]

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[TiposOrganizacion](
	[IdTipoOrg] [int] IDENTITY(1,1) NOT NULL,
	[Nombre] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Activo] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdTipoOrg] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET IDENTITY_INSERT [dbo].[TiposOrganizacion] ON 

INSERT [dbo].[TiposOrganizacion] ([IdTipoOrg], [Nombre], [Activo]) VALUES (1, N'Iglesia Local', 1)
INSERT [dbo].[TiposOrganizacion] ([IdTipoOrg], [Nombre], [Activo]) VALUES (2, N'Misión / Extensión', 1)
INSERT [dbo].[TiposOrganizacion] ([IdTipoOrg], [Nombre], [Activo]) VALUES (3, N'Ministerio Paraeclesiástico', 1)
INSERT [dbo].[TiposOrganizacion] ([IdTipoOrg], [Nombre], [Activo]) VALUES (4, N'Fundación / ONG', 0)
INSERT [dbo].[TiposOrganizacion] ([IdTipoOrg], [Nombre], [Activo]) VALUES (5, N'Colegio Cristiano', 1)
INSERT [dbo].[TiposOrganizacion] ([IdTipoOrg], [Nombre], [Activo]) VALUES (6, N'Escuela / Liceo', 1)
SET IDENTITY_INSERT [dbo].[TiposOrganizacion] OFF
ALTER TABLE [dbo].[TiposOrganizacion] ADD  DEFAULT ((1)) FOR [Activo]

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[TransferenciasEquipo](
	[IdTransferencia] [int] IDENTITY(1,1) NOT NULL,
	[NumeroConstancia] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[IdTemporada] [int] NOT NULL,
	[IdEquipo] [int] NOT NULL,
	[IdAlmacenOrigen] [int] NOT NULL,
	[FechaTransferencia] [datetime2](7) NOT NULL,
	[CoordinadorEmisor] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[PersonaReceptoraEquipo] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Observaciones] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Estado] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[IdUsuarioRegistro] [int] NOT NULL,
	[FechaRegistro] [datetime2](7) NOT NULL,
	[IdEquipoEmisor] [int] NULL,
	[FechaEmision] [datetime2](7) NULL,
	[FechaRecepcion] [datetime2](7) NULL,
	[IdUsuarioEmisor] [int] NULL,
	[IdUsuarioReceptor] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdTransferencia] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
SET IDENTITY_INSERT [dbo].[TransferenciasEquipo] ON 

INSERT [dbo].[TransferenciasEquipo] ([IdTransferencia], [NumeroConstancia], [IdTemporada], [IdEquipo], [IdAlmacenOrigen], [FechaTransferencia], [CoordinadorEmisor], [PersonaReceptoraEquipo], [Observaciones], [Estado], [IdUsuarioRegistro], [FechaRegistro], [IdEquipoEmisor], [FechaEmision], [FechaRecepcion], [IdUsuarioEmisor], [IdUsuarioReceptor]) VALUES (1, N'TEST-CTE-3878', 1, 1, 1, CAST(N'2026-09-02T02:54:13.2000000' AS DateTime2), N'Emisor Central', N'Receptor Equipo', NULL, N'COMPLETADA', 1, CAST(N'2026-09-02T02:54:13.2000000' AS DateTime2), NULL, NULL, NULL, NULL, NULL)
INSERT [dbo].[TransferenciasEquipo] ([IdTransferencia], [NumeroConstancia], [IdTemporada], [IdEquipo], [IdAlmacenOrigen], [FechaTransferencia], [CoordinadorEmisor], [PersonaReceptoraEquipo], [Observaciones], [Estado], [IdUsuarioRegistro], [FechaRegistro], [IdEquipoEmisor], [FechaEmision], [FechaRecepcion], [IdUsuarioEmisor], [IdUsuarioReceptor]) VALUES (2, N'TEST-CTE-9213', 1, 1, 1, CAST(N'2026-09-02T02:54:51.0000000' AS DateTime2), N'Emisor Central', N'Receptor Equipo', NULL, N'COMPLETADA', 1, CAST(N'2026-09-02T02:54:51.0000000' AS DateTime2), NULL, NULL, NULL, NULL, NULL)
INSERT [dbo].[TransferenciasEquipo] ([IdTransferencia], [NumeroConstancia], [IdTemporada], [IdEquipo], [IdAlmacenOrigen], [FechaTransferencia], [CoordinadorEmisor], [PersonaReceptoraEquipo], [Observaciones], [Estado], [IdUsuarioRegistro], [FechaRegistro], [IdEquipoEmisor], [FechaEmision], [FechaRecepcion], [IdUsuarioEmisor], [IdUsuarioReceptor]) VALUES (3, N'TEST-CTE-7221', 1, 1, 1, CAST(N'2026-09-02T02:55:11.8733333' AS DateTime2), N'Emisor Central', N'Receptor Equipo', NULL, N'COMPLETADA', 1, CAST(N'2026-09-02T02:55:11.8733333' AS DateTime2), NULL, NULL, NULL, NULL, NULL)
INSERT [dbo].[TransferenciasEquipo] ([IdTransferencia], [NumeroConstancia], [IdTemporada], [IdEquipo], [IdAlmacenOrigen], [FechaTransferencia], [CoordinadorEmisor], [PersonaReceptoraEquipo], [Observaciones], [Estado], [IdUsuarioRegistro], [FechaRegistro], [IdEquipoEmisor], [FechaEmision], [FechaRecepcion], [IdUsuarioEmisor], [IdUsuarioReceptor]) VALUES (4, N'TEST-TRF-3565', 1, 1, 1, CAST(N'2026-09-02T03:29:28.4233333' AS DateTime2), N'Coordinador Central', N'Lider Equipo', NULL, N'COMPLETADA', 1, CAST(N'2026-09-02T03:29:28.4233333' AS DateTime2), NULL, NULL, NULL, NULL, NULL)
INSERT [dbo].[TransferenciasEquipo] ([IdTransferencia], [NumeroConstancia], [IdTemporada], [IdEquipo], [IdAlmacenOrigen], [FechaTransferencia], [CoordinadorEmisor], [PersonaReceptoraEquipo], [Observaciones], [Estado], [IdUsuarioRegistro], [FechaRegistro], [IdEquipoEmisor], [FechaEmision], [FechaRecepcion], [IdUsuarioEmisor], [IdUsuarioReceptor]) VALUES (5, N'TRF-20260902-6385', 1, 2, 2, CAST(N'2026-09-02T00:00:00.0000000' AS DateTime2), N'Efesos Astacio', N'CMI Rol', N'', N'RECIBIDA', 1, CAST(N'2026-09-02T19:46:05.6500000' AS DateTime2), 24, CAST(N'2026-09-02T00:00:00.0000000' AS DateTime2), CAST(N'2026-09-03T00:00:00.0000000' AS DateTime2), NULL, NULL)
INSERT [dbo].[TransferenciasEquipo] ([IdTransferencia], [NumeroConstancia], [IdTemporada], [IdEquipo], [IdAlmacenOrigen], [FechaTransferencia], [CoordinadorEmisor], [PersonaReceptoraEquipo], [Observaciones], [Estado], [IdUsuarioRegistro], [FechaRegistro], [IdEquipoEmisor], [FechaEmision], [FechaRecepcion], [IdUsuarioEmisor], [IdUsuarioReceptor]) VALUES (6, N'TRF-20260902-2659', 1, 2, 2, CAST(N'2026-09-02T00:00:00.0000000' AS DateTime2), N'Efesos Astacio', N'CD Rol', N'', N'RECIBIDA', 1, CAST(N'2026-09-02T19:55:20.2266667' AS DateTime2), 24, CAST(N'2026-09-02T00:00:00.0000000' AS DateTime2), CAST(N'2026-09-02T00:00:00.0000000' AS DateTime2), NULL, NULL)
SET IDENTITY_INSERT [dbo].[TransferenciasEquipo] OFF
SET ANSI_PADDING ON

ALTER TABLE [dbo].[TransferenciasEquipo] ADD UNIQUE NONCLUSTERED 
(
	[NumeroConstancia] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[TransferenciasEquipo] ADD  DEFAULT (getdate()) FOR [FechaTransferencia]
ALTER TABLE [dbo].[TransferenciasEquipo] ADD  DEFAULT ('COMPLETADA') FOR [Estado]
ALTER TABLE [dbo].[TransferenciasEquipo] ADD  DEFAULT (getdate()) FOR [FechaRegistro]
ALTER TABLE [dbo].[TransferenciasEquipo]  WITH CHECK ADD FOREIGN KEY([IdAlmacenOrigen])
REFERENCES [dbo].[Almacenes] ([IdAlmacen])
ALTER TABLE [dbo].[TransferenciasEquipo]  WITH CHECK ADD FOREIGN KEY([IdEquipoEmisor])
REFERENCES [dbo].[Equipos] ([IdEquipo])
ALTER TABLE [dbo].[TransferenciasEquipo]  WITH CHECK ADD FOREIGN KEY([IdEquipo])
REFERENCES [dbo].[Equipos] ([IdEquipo])
ALTER TABLE [dbo].[TransferenciasEquipo]  WITH CHECK ADD FOREIGN KEY([IdTemporada])
REFERENCES [dbo].[Temporadas] ([IdTemporada])
ALTER TABLE [dbo].[TransferenciasEquipo]  WITH CHECK ADD FOREIGN KEY([IdUsuarioEmisor])
REFERENCES [dbo].[Usuarios] ([IdUsuario])
ALTER TABLE [dbo].[TransferenciasEquipo]  WITH CHECK ADD FOREIGN KEY([IdUsuarioReceptor])
REFERENCES [dbo].[Usuarios] ([IdUsuario])
ALTER TABLE [dbo].[TransferenciasEquipo]  WITH CHECK ADD FOREIGN KEY([IdUsuarioRegistro])
REFERENCES [dbo].[Usuarios] ([IdUsuario])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[TransferenciasEquipoDetalle](
	[IdTransferenciaDetalle] [int] IDENTITY(1,1) NOT NULL,
	[IdTransferencia] [int] NOT NULL,
	[IdMaterial] [int] NOT NULL,
	[CantidadUnidades] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdTransferenciaDetalle] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET IDENTITY_INSERT [dbo].[TransferenciasEquipoDetalle] ON 

INSERT [dbo].[TransferenciasEquipoDetalle] ([IdTransferenciaDetalle], [IdTransferencia], [IdMaterial], [CantidadUnidades]) VALUES (1, 1, 1, 40)
INSERT [dbo].[TransferenciasEquipoDetalle] ([IdTransferenciaDetalle], [IdTransferencia], [IdMaterial], [CantidadUnidades]) VALUES (2, 2, 1, 40)
INSERT [dbo].[TransferenciasEquipoDetalle] ([IdTransferenciaDetalle], [IdTransferencia], [IdMaterial], [CantidadUnidades]) VALUES (3, 3, 1, 40)
INSERT [dbo].[TransferenciasEquipoDetalle] ([IdTransferenciaDetalle], [IdTransferencia], [IdMaterial], [CantidadUnidades]) VALUES (4, 4, 3, 60)
INSERT [dbo].[TransferenciasEquipoDetalle] ([IdTransferenciaDetalle], [IdTransferencia], [IdMaterial], [CantidadUnidades]) VALUES (5, 5, 3, 7200)
INSERT [dbo].[TransferenciasEquipoDetalle] ([IdTransferenciaDetalle], [IdTransferencia], [IdMaterial], [CantidadUnidades]) VALUES (6, 6, 3, 7200)
SET IDENTITY_INSERT [dbo].[TransferenciasEquipoDetalle] OFF
ALTER TABLE [dbo].[TransferenciasEquipoDetalle]  WITH CHECK ADD FOREIGN KEY([IdMaterial])
REFERENCES [dbo].[Materiales] ([IdMaterial])
ALTER TABLE [dbo].[TransferenciasEquipoDetalle]  WITH CHECK ADD FOREIGN KEY([IdTransferencia])
REFERENCES [dbo].[TransferenciasEquipo] ([IdTransferencia])

GO
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Usuarios](
	[IdUsuario] [int] IDENTITY(1,1) NOT NULL,
	[Correo] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Clave] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[IdRolSeguridad] [int] NOT NULL,
	[IdEstado] [int] NOT NULL,
	[TokenRecuperacion] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ExpiracionTokenRecuperacion] [datetime] NULL,
	[IntentosFallidosToken] [int] NULL,
	[FechaRegistro] [datetime] NULL,
	[FechaUltimoAcceso] [datetime] NULL,
	[IntentosFallidosLogin] [int] NOT NULL,
	[FechaUltimoIntentoFallido] [datetime] NULL,
	[FechaBloqueo] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdUsuario] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
SET IDENTITY_INSERT [dbo].[Usuarios] ON 

INSERT [dbo].[Usuarios] ([IdUsuario], [Correo], [Clave], [IdRolSeguridad], [IdEstado], [TokenRecuperacion], [ExpiracionTokenRecuperacion], [IntentosFallidosToken], [FechaRegistro], [FechaUltimoAcceso], [IntentosFallidosLogin], [FechaUltimoIntentoFallido], [FechaBloqueo]) VALUES (1, N'admin@occrd.org', N'p3LAA9q/hzks5x7lJfl8SQ==:9sSnD2FIlEym6HOgMkKgbIGU3rPt2SEoS2vuPFut76Y=', 1, 4, NULL, NULL, 0, CAST(N'2026-08-20T18:15:59.520' AS DateTime), NULL, 0, NULL, NULL)
INSERT [dbo].[Usuarios] ([IdUsuario], [Correo], [Clave], [IdRolSeguridad], [IdEstado], [TokenRecuperacion], [ExpiracionTokenRecuperacion], [IntentosFallidosToken], [FechaRegistro], [FechaUltimoAcceso], [IntentosFallidosLogin], [FechaUltimoIntentoFallido], [FechaBloqueo]) VALUES (2, N'CMI@correo.com', N'biM/+syoPLN/lfu2YY/yKA==:MnZjR0BFGbTb/GYMfGC0dOOu9Sz7rDdrA9DOohaEIBM=', 3, 4, NULL, NULL, 0, CAST(N'2026-08-24T22:12:16.487' AS DateTime), NULL, 0, NULL, NULL)
INSERT [dbo].[Usuarios] ([IdUsuario], [Correo], [Clave], [IdRolSeguridad], [IdEstado], [TokenRecuperacion], [ExpiracionTokenRecuperacion], [IntentosFallidosToken], [FechaRegistro], [FechaUltimoAcceso], [IntentosFallidosLogin], [FechaUltimoIntentoFallido], [FechaBloqueo]) VALUES (3, N'cd@correo.com', N'NQE4FGbEmQg83vG8YJmjPA==:xHw1FChm8D8yJMxNyRm37t9FB7xeBWW2Sjl5TB/Vg5U=', 3, 4, NULL, NULL, 0, CAST(N'2026-08-24T22:12:27.270' AS DateTime), NULL, 0, NULL, NULL)
INSERT [dbo].[Usuarios] ([IdUsuario], [Correo], [Clave], [IdRolSeguridad], [IdEstado], [TokenRecuperacion], [ExpiracionTokenRecuperacion], [IntentosFallidosToken], [FechaRegistro], [FechaUltimoAcceso], [IntentosFallidosLogin], [FechaUltimoIntentoFallido], [FechaBloqueo]) VALUES (4, N'cm@erle.com', N'I9bOhTrCjE3FEMwyeVLiXQ==:zCB+D5giKmp6QjLxZDoC1IrrST8tQVE4KyK2lHvCq8c=', 3, 4, NULL, NULL, 0, CAST(N'2026-09-01T21:05:33.687' AS DateTime), NULL, 0, NULL, NULL)
SET IDENTITY_INSERT [dbo].[Usuarios] OFF
SET ANSI_PADDING ON

ALTER TABLE [dbo].[Usuarios] ADD UNIQUE NONCLUSTERED 
(
	[Correo] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Usuarios_UnicoSuperAdminActivo] ON [dbo].[Usuarios]
(
	[IdRolSeguridad] ASC
)
WHERE ([IdRolSeguridad]=(1) AND [IdEstado]=(4))
WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT ((3)) FOR [IdRolSeguridad]
ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT ((1)) FOR [IdEstado]
ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT ((0)) FOR [IntentosFallidosToken]
ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT (getdate()) FOR [FechaRegistro]
ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT ((0)) FOR [IntentosFallidosLogin]
ALTER TABLE [dbo].[Usuarios]  WITH CHECK ADD FOREIGN KEY([IdEstado])
REFERENCES [dbo].[EstadosCuenta] ([IdEstado])
ALTER TABLE [dbo].[Usuarios]  WITH CHECK ADD FOREIGN KEY([IdRolSeguridad])
REFERENCES [dbo].[RolesSeguridad] ([IdRolSeguridad])

GO
