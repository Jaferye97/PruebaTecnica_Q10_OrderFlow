# 📦 OrderFlow - Prueba Técnica Q10

## 🧾 Descripción

Sistema para registrar pedidos y reservar inventario. Está compuesto por dos servicios en .NET 10 que se comunican por eventos a través de RabbitMQ, y un panel web para crear pedidos y ver en qué estado va cada uno.

- **Orders API**: valida el pedido, lo guarda como `Pending` y publica `OrderCreated` usando un Outbox.
- **Inventory Worker**: consume `OrderCreated`, descuenta el stock y responde `StockReserved` o `StockRejected`.
- Orders recibe la respuesta y deja el pedido en `Confirmed` o `Rejected`.
- **Frontend**: React con TanStack Query, con el formulario de pedidos y la lista que se actualiza sola.


---

## 🚀 Características

- ➕ Creación de pedidos con validación en el formulario y en el backend: nombre obligatorio, SKU existente y cantidad entre 1 y 100. Los errores del servidor aparecen debajo de cada campo.
- 🔄 Lista de pedidos que se refresca sola: cada 2 s mientras haya pedidos `Pending` y cada 15 s cuando no. Paginada de 10 en 10.
- 📨 Mensajería con Outbox, publisher confirms, colas quorum durables y dead-letter queue.
- 🔁 Idempotencia en el consumidor: si el mismo evento llega dos veces, el stock se descuenta una sola vez.
- 🛡️ Concurrencia optimista con `RowVersion` y un `CHECK` en la base para que el stock nunca quede negativo.
- 🐳 Todo el sistema se levanta con `docker compose up`, incluidos los datos de prueba.
- 🧪 51 tests de backend (xUnit) y 30 de frontend (Vitest).

---

## ⚙️ Tecnologías Usadas

| Backend | Frontend | Infraestructura |
|---|---|---|
| .NET 10 · C# · Minimal APIs | React 19 · TypeScript | Docker Compose |
| Entity Framework Core 10 | Vite 7 | SQL Server 2022 |
| RabbitMQ.Client 7 | TanStack Query 5 | RabbitMQ 4 (management) |
| OpenAPI + Scalar | TanStack Table 8 | nginx |
| xUnit + SQLite en memoria | Vitest 4 + Testing Library | Imágenes .NET chiseled |

---

## 💡 Cómo Ejecutarlo

### Requisitos

- Docker Desktop (con 4 GB de RAM o más para SQL Server).
- Para correr los tests o trabajar sin Docker: .NET SDK 10 y Node 20.19 o superior.

### 🐳 Con Docker

```bash
docker compose up --build
```

No es necesario crear el `.env`. Si no existe, el `docker-compose.yml` usa los mismos valores de [`.env.example`](.env.example). Para usar contraseñas propias, copia `.env.example` a `.env` y cámbialas (la de SQL Server necesita mínimo 8 caracteres con mayúscula, minúscula, número y símbolo).

Las bases, las tablas y los datos de prueba se crean con los scripts de [`db/init/`](db/init). El servicio `db-init` los ejecuta antes de que arranquen la API y el worker, y como son idempotentes se pueden ejecutar en cada `up` sin duplicar datos. No se usan migraciones de EF.

El primer build tarda unos minutos porque descarga las imágenes base. Para empezar de cero: `docker compose down -v`.

> ⚠️ SQL Server y RabbitMQ toman la contraseña solo al crear su volumen. Si cambias el `.env` después del primer `up`, ejecuta `docker compose down -v`.

| Qué | URL | Credenciales |
|---|---|---|
| Panel | http://localhost:3000 | |
| API + documentación (Scalar) | http://localhost:8080/scalar | |
| Health checks | http://localhost:8080/health/live y `/health/ready` | |
| RabbitMQ Management | http://localhost:15672 | las del `.env` (sin `.env`: `guest_user` / `change_me`) |
| SQL Server | `localhost,1433` | `sa` / la del `.env` (sin `.env`: `YourStrong!Passw0rd`) |

### 🌱 Productos de prueba

Los mismos SKU están cargados en los dos servicios (`OrdersDb.Products` e `InventoryDb.Stock`):

| SKU | Producto | Stock | Para probar |
|---|---|---|---|
| `SKU-001` | Teclado mecánico | 100 | pedido normal, queda `Confirmed` |
| `SKU-002` | Mouse inalámbrico | 10 | pedir 11, queda `Rejected` por stock |
| `SKU-003` | Monitor 27 pulgadas | 2 | dos pedidos lo agotan, el tercero se rechaza |
| `SKU-004` | Audífonos | 0 | siempre `Rejected` |

### 🔌 Endpoints

| Método | Ruta | Respuesta |
|---|---|---|
| `POST` | `/api/orders` | `201` con header `Location`, o `400` si los datos son inválidos |
| `GET` | `/api/orders?take=50` | pedidos más recientes primero (`take` de 1 a 200, por defecto 50) |
| `GET` | `/api/orders/{id}` | `200`, o `404` si no existe |
| `GET` | `/api/products` | catálogo para el formulario |

```bash
curl -i -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -d '{"nombreCliente":"Ana","sku":"SKU-001","cantidad":1}'

curl http://localhost:8080/api/orders/<id>     # pasa de Pending a Confirmed en ~2 s
```

Los errores se devuelven en formato ProblemDetails, agrupados por campo:

```json
{
  "status": 400,
  "title": "La solicitud tiene errores de validación",
  "errors": {
    "nombreCliente": ["El nombre del cliente es obligatorio."],
    "cantidad": ["La cantidad debe estar entre 1 y 100."],
    "sku": ["El SKU 'SKU-999' no existe en el catálogo."]
  },
  "codes": ["order.nombre_requerido", "order.cantidad_invalida", "order.sku_no_existe"]
}
```

### 🧑‍💻 Sin Docker (opcional)

Levanta solo la base de datos y el broker:

```bash
docker compose up -d db-init rabbitmq
```

Luego ejecuta cada servicio desde el IDE o con `dotnet run`, con estas variables de entorno (la cadena de conexión apunta a `localhost,1433`):

| Servicio | Variables |
|---|---|
| Orders API (`backend/Orders.Infrastructure.In.WebApi`, perfil `http`, puerto 5109) | `ConnectionStrings__OrdersDb`, `RabbitMq__User`, `RabbitMq__Password` |
| Inventory Worker (`backend/Inventory.Infrastructure.In.Worker`) | `ConnectionStrings__InventoryDb`, `RabbitMq__User`, `RabbitMq__Password` |

Frontend: `cd frontend`, `npm install` y `npm run dev` (http://localhost:5173). El proxy de Vite manda `/api` a `http://localhost:8080`; si la API corre con `dotnet run`, cambia el `target` de `vite.config.ts` a `5109`.

---

## 🧪 Tests

| Qué | Comando | Tests |
|---|---|---|
| Backend (xUnit) | `dotnet test backend/OrderFlow.slnx` | 51 (Orders 35 + Inventory 16) |
| Frontend (Vitest) | `cd frontend && npm test` | 30 |

Ninguno necesita Docker. Cubren:

- Dominio: las transiciones de estado del pedido, las validaciones de `Order.Create` y la regla de reserva de stock.
- Idempotencia: el mismo mensaje entregado dos veces, cada vez con su propio `DbContext`, descuenta el stock una sola vez (`ReserveStockSqliteTests`).
- Handlers con fakes: que se publique después de guardar, que no se publique nada si falla el guardado y que un duplicado republique la misma respuesta.
- Outbox: el pedido y su evento se guardan juntos, y el dispatcher se detiene cuando falla una publicación.
- Configuración: arranque sin cadena de conexión, `RowVersion`, índices, longitudes de columnas, enums como texto y validación de las opciones de RabbitMQ.
- Frontend: validación del formulario, intervalos de polling, cliente HTTP, errores en pantalla sin llamar a la API y paginación de la tabla.

---

## 🛠️ Estructura del Proyecto

```
/db/init
├── 01-orders.sql        (OrdersDb: Products, Orders, OutboxMessages y datos de prueba)
└── 02-inventory.sql     (InventoryDb: Stock, ProcessedEvents y datos de prueba)

/backend                                  (OrderFlow.slnx)
├── OrderFlow.Contracts                   eventos de integración, exchange y routing keys
├── Orders.Domain                         entidad Order y sus reglas
├── Orders.Application                    commands, queries, dispatcher del Outbox y puertos
├── Orders.Infrastructure.Out.SqlServer   EF Core: repositorios, queries, Outbox, UnitOfWork
├── Orders.Infrastructure.Out.RabbitMq    conexión, topología y publicador
├── Orders.Infrastructure.In.WebApi       endpoints, relay del Outbox y consumidor de resultados
├── Inventory.Domain                      entidad Stock
├── Inventory.Application                 ReserveStockHandler y puertos
├── Inventory.Infrastructure.Out.*        SqlServer y RabbitMq
├── Inventory.Infrastructure.In.Worker    Worker y consumidor de OrderCreated
└── Orders.Tests / Inventory.Tests

/frontend/src
├── api                     contratos y cliente HTTP
├── features/orders
│   ├── components          OrderForm, OrdersTable, TablePagination, StatusBadge
│   ├── hooks               useProducts, useCreateOrder, useOrders
│   └── validation.ts       mismas reglas que el backend
└── test                    utilidades de Vitest
```

---

## 🏛️ Decisiones de Arquitectura

El criterio que guió las decisiones fue la proporcionalidad: resolver bien un problema de negocio simple (crear un pedido y reservar stock) sin agregar complejidad que no se pueda justificar. En cada caso se compararon alternativas; abajo queda qué se eligió, qué se descartó y qué costo se asumió.

### Resumen

| # | Tema | Decisión | Alternativas descartadas |
|---|------|----------|--------------------------|
| 1 | Estructura del repositorio | Monorepo: `backend/`, `frontend/` y la orquestación en la raíz | Repos separados; compose dentro de `backend/` |
| 2 | Plataforma | .NET 10 (LTS) | .NET 8 (fin de soporte en noviembre de 2026) |
| 3 | Arquitectura | Hexagonal (puertos y adaptadores) con proyectos `In` y `Out` | Capas tradicionales; un proyecto por servicio |
| 4 | Nombres de adaptadores | Por tecnología: `Out.SqlServer`, `Out.RabbitMq` | Por rol: `Out.Persistence`, `Out.Messaging` |
| 5 | CQRS | Ligero, sin MediatR, solo en Orders | MediatR; CQRS en ambos servicios; Event Sourcing |
| 6 | Base de datos | SQL Server en Docker: una instancia, una base por servicio | PostgreSQL; SQLite; base en memoria |
| 7 | Validación de SKU | Catálogo `Products` replicado en OrdersDb | Llamada HTTP a Inventory; aceptar cualquier SKU |
| 8 | Mensajería | RabbitMQ.Client v7 directo + patrón Outbox | MassTransit; cola en memoria |
| 9 | Idempotencia | Tabla `ProcessedEvents` con `EventId` como PK, en la misma transacción del descuento | Deduplicación en memoria; confiar en el broker |
| 10 | Frontend | React + Vite + TanStack Query | Angular |
| 11 | Actualización del panel | Polling rápido solo mientras existan pedidos `Pending` | Polling fijo; SignalR desde el inicio |
| 12 | Tests | xUnit + SQLite en memoria; Vitest en el frontend | Mocks de repositorios; Testcontainers |

### 1. Monorepo con `backend/` y `frontend/`

- **Decisión:** un único repositorio. El código .NET va en `backend/`, la aplicación React en `frontend/`, y lo que pertenece al sistema completo (`docker-compose.yml`, `.env.example`, `db/init/` y este README) queda en la raíz.
- **Por qué:** quien evalúa clona el repositorio y ejecuta `docker compose up` sin entrar a ninguna carpeta. Los tests de backend también se corren desde la raíz con un solo comando (`dotnet test backend/OrderFlow.slnx`).

### 2. .NET 10

- **Decisión:** .NET 10, la versión LTS vigente.
- **Por qué:** .NET 8 terminó su soporte en noviembre de 2026. Entregar sobre una versión sin soporte sería una deuda técnica desde el primer día.

### 3. Arquitectura hexagonal con `In` y `Out`

- **Decisión:** cada servicio tiene un núcleo (`Domain` + `Application`) y adaptadores separados en proyectos:
  - **In** (adaptadores que disparan la aplicación): `Orders...In.WebApi` (HTTP) e `Inventory...In.Worker` (mensajes).
  - **Out** (adaptadores que la aplicación usa a través de puertos): `Out.SqlServer` y `Out.RabbitMq`.
- **Por qué:** hace explícita la dirección de las dependencias. `Application` define interfaces (puertos) como `IOrderRepository`, `IOutbox` o `IMessagePublisher`, y los adaptadores `Out` las implementan. El dominio no conoce EF Core ni RabbitMQ.
- **Reglas que se cumplen y el compilador protege:**
  - `Domain` y `Contracts` no referencian ningún proyecto.
  - `Application` nunca referencia un `Out`.
  - `Out.SqlServer` y `Out.RabbitMq` no se referencian entre sí.
  - Ningún proyecto referencia un `In`. Los `In` son los ejecutables y actúan como *composition root*: solo registran las implementaciones por inyección de dependencias.
- **Trade-off asumido:** 13 proyectos para un dominio pequeño. Se aceptó porque cada proyecto tiene una sola responsabilidad y la estructura se entiende con solo leer los nombres.

### 4. Adaptadores nombrados por tecnología

- **Decisión:** `Out.SqlServer` y `Out.RabbitMq`, en vez de `Out.Persistence` y `Out.Messaging`.
- **Por qué:** en hexagonal, el **puerto** representa el rol y el **adaptador** es una implementación concreta con una tecnología. Si mañana se migrara a PostgreSQL, no se renombra nada: se agrega un adaptador `Out.Postgres` que implementa los mismos puertos.

### 5. CQRS ligero, sin MediatR y solo en Orders

- **Decisión:** en `Orders.Application`, los casos de uso que escriben (`CreateOrder`, `ConfirmOrder`, `RejectOrder`) están separados de los que leen (`GetOrders`, `GetOrderById`), cada uno con su propio handler. No se usa MediatR.
- **Por qué no en Inventory:** el Worker no expone consultas; su único caso de uso es reservar stock. Separar commands y queries ahí no aporta nada.
- **Por qué sin MediatR:** para estos pocos casos de uso, un paquete adicional no se justifica. Además, MediatR pasó a licencia comercial para empresas.
- **Relación con los eventos:** CQRS y los eventos de integración son conceptos distintos y complementarios. Los eventos **entran** por consumidores delgados que invocan un command handler, y **salen** a través del Outbox que llena un command. Se descartó **Event Sourcing** porque el problema no requiere reconstruir el historial de estados.

### 6. SQL Server en Docker, una base por servicio

- **Decisión:** una instancia de SQL Server 2022 en el compose, con dos bases: `OrdersDb` (de Orders) e `InventoryDb` (de Inventory). El esquema y el stock inicial se crean con los scripts SQL de `db/init/`, que ejecuta el servicio `db-init` al levantar el sistema. No se usan migraciones de EF.
- **Por qué:** en un sistema distribuido, cada servicio es dueño de sus datos; ningún servicio lee la base del otro. Se usa una sola instancia para no duplicar el consumo de recursos en local. Con scripts, el esquema queda versionado y visible, y el seed corre solo.
- **Trade-off asumido:** la imagen pesa alrededor de 2 GB y tarda en arrancar. Se mitiga con `healthcheck` y `depends_on`: `db-init` espera a que SQL Server esté sano, y la API y el worker esperan a que `db-init` termine bien. Como nadie compara automáticamente el modelo de EF con los scripts, esa coincidencia la protegen los tests de configuración. Cambiar a PostgreSQL implica cambiar el proveedor de EF y reescribir los scripts, no la lógica.

### 7. Catálogo replicado para validar el SKU

- **Contexto:** el enunciado exige responder **400** si el SKU no existe, pero el stock pertenece a InventoryDb.
- **Decisión:** OrdersDb tiene una tabla `Products` con los mismos SKUs del seed. Orders valida contra su propio catálogo, que solo indica qué productos existen; la disponibilidad sigue siendo responsabilidad de Inventory.
- **Alternativas descartadas:**
  - *Llamada HTTP a Inventory:* crea acoplamiento síncrono; si Inventory cae, no se pueden crear pedidos.
  - *Aceptar cualquier SKU y que el Worker lo rechace:* incumple el requisito del 400 al crear.
- **Mejora futura:** sincronizar el catálogo con un evento `ProductCreated` publicado por Inventory.

### 8. RabbitMQ.Client v7 directo + patrón Outbox

- **Decisión:** comunicación asíncrona por RabbitMQ usando la librería oficial (API async de la v7), con un Outbox implementado a mano en Orders.
- **Por qué no MassTransit:** resuelve gran parte de la prueba por sí solo (reintentos, outbox, topología). Se prefirió que el manejo de mensajería y fallos mostrara criterio propio y se pudiera explicar línea por línea.
- **Cómo funciona el Outbox:** al crear un pedido, el registro en `Orders` y el evento `OrderCreated` en `OutboxMessages` se guardan **en la misma transacción**. Un proceso en segundo plano publica los mensajes pendientes con *publisher confirms* y los marca como enviados.
- **Trade-off asumido:** más código propio y más tiempo de desarrollo a cambio de control total y trazabilidad.

### 9. Idempotencia en el consumidor

- **Decisión:** Inventory registra cada `EventId` procesado en `ProcessedEvents` (PK), **en la misma transacción** que descuenta el stock. Si el evento llega dos veces, la segunda vez se detecta y no se descuenta de nuevo.
- **Concurrencia:** la tabla de stock usa un token de concurrencia (`rowversion`) para evitar sobreventa con pedidos simultáneos del mismo SKU.
- **Del lado de Orders:** actualizar el estado es idempotente por diseño, porque un pedido solo transiciona desde `Pending`. Un `StockReserved` duplicado sobre un pedido ya `Confirmed` no tiene efecto.
- **Se demuestra con** un test que procesa el mismo evento dos veces y verifica que el stock solo baja una vez, y con la prueba manual de más abajo.

### 10. Manejo de fallos

| Escenario | Qué pasa |
|---|---|
| **El broker está caído cuando Orders crea un pedido** | El pedido y el evento quedan guardados juntos (Outbox). El pedido permanece `Pending` y el evento se publica automáticamente cuando RabbitMQ vuelve. No se pierde nada ni se devuelve error al usuario. |
| **Inventory no responde** | `OrderCreated` espera en una cola durable. El pedido sigue `Pending` hasta que el Worker vuelva y procese el mensaje. |
| **Un mensaje falla repetidamente** | Tras 5 intentos (`x-delivery-limit`) se envía a una *dead-letter queue* para revisión, en vez de reintentarse indefinidamente. Los mensajes que nunca se van a poder procesar (JSON inválido, datos incompletos) van directo a la DLQ. |
| **Inventory descuenta el stock y cae antes de publicar la respuesta** | El resultado ya quedó guardado en `ProcessedEvents` y el mensaje original no tuvo ack, así que RabbitMQ lo vuelve a entregar. El Worker encuentra el `EventId`, no descuenta otra vez y republica la respuesta guardada con el mismo `ResultEventId`. |

Más abajo están los comandos para reproducir cada caso.

### 11. Frontend: React + Vite + TanStack Query

- **Decisión:** React con Vite, y TanStack Query para el estado del servidor.
- **Por qué:** TanStack Query resuelve caché, reintentos e invalidación sin código manual, y deja los componentes enfocados en la presentación.
- **Errores visibles:** la validación ocurre en dos capas. La del cliente mejora la experiencia; la del servidor (400 con `ProblemDetails`) es la que manda, y sus errores se muestran junto a cada campo del formulario.
- **Despliegue:** build multi-stage (`node` para compilar, `nginx` para servir). Nginx hace de proxy de `/api` hacia la API, lo que evita configurar CORS.

### 12. Polling condicional, SignalR como siguiente paso

- **Decisión:** la lista se refresca cada 2 segundos **solo mientras exista algún pedido `Pending`**. Cuando todos están resueltos, baja a cada 15 segundos, suficiente para ver pedidos creados desde otro navegador.
- **Por qué:** el enunciado acepta polling, y esta variante evita peticiones innecesarias. SignalR se dejó como mejora para priorizar la solidez de la mensajería.

### 13. Configuración por variables de entorno

- **Decisión:** ninguna cadena de conexión ni secreto está en el código. Docker Compose lee el `.env` (no versionado; se incluye `.env.example`) e inyecta variables como `ConnectionStrings__OrdersDb` en cada contenedor. ASP.NET Core las carga automáticamente y traduce `__` a `:`. Si no hay `.env`, el compose usa los valores de ejemplo para que el sistema arranque igual.
- **Dentro de Docker,** los servicios se conectan por nombre de servicio (`Server=sqlserver`), no por `localhost`.
- **Trade-off asumido:** la aplicación usa el usuario `sa` por simplicidad. En producción se crearía un usuario con permisos mínimos por base de datos y los secretos irían en un gestor de secretos.

### 14. Tests con xUnit y SQLite en memoria

- **Decisión:** tests sobre la lógica crítica: validación, transiciones de estado, Outbox en la misma transacción, idempotencia y rechazo por stock insuficiente, más tests de configuración. En el frontend, Vitest con Testing Library.
- **Por qué SQLite en memoria y no mocks:** las restricciones reales (PK única de `ProcessedEvents`, transacciones) se prueban de verdad. Un mock no detectaría una violación de PK.
- **Por qué no Testcontainers:** levantar SQL Server en cada corrida hace los tests lentos; SQLite cubre las garantías que se necesitan verificar.
- **Aserciones:** con `Assert` de xUnit, para evitar dependencias con licencia comercial.

---

## 🧯 Probar los Fallos

Estos son los dos casos que pide el enunciado. Se probaron deteniendo los servicios a mano.

### RabbitMQ caído al crear un pedido

```bash
docker compose stop rabbitmq
curl -i -X POST http://localhost:8080/api/orders -H "Content-Type: application/json" \
  -d '{"nombreCliente":"Ana","sku":"SKU-001","cantidad":1}'     # responde 201, Pending
docker compose start rabbitmq                                   # a los segundos queda Confirmed
```

Mientras el broker estuvo caído, el mensaje del Outbox llegó a 6 intentos fallidos sin perderse. Unos 7 segundos después de arrancar RabbitMQ el pedido ya estaba `Confirmed`.

### Inventory caído

```bash
docker compose stop inventory-worker    # los pedidos nuevos se quedan Pending
                                        # en :15672 > Queues, inventory.order-created acumula mensajes
docker compose start inventory-worker   # la cola se vacía y los pedidos se resuelven
```

La cola llegó a tener 2 mensajes sin consumidores; al volver el worker, los dos pedidos quedaron `Confirmed`.

### Mensaje inválido

En http://localhost:15672, entra a Exchanges, `orderflow.events`, *Publish message*, con routing key `order.created` y payload `{"esto":"no es un pedido"}`. El mensaje termina en `inventory.order-created.dlq` y la cola principal sigue funcionando.

---

## 🔁 Demostrar la Idempotencia

La idea es forzar que el Outbox publique otra vez el mismo evento y comprobar que el stock solo se descontó una vez. Los comandos son de bash (en Windows funcionan en Git Bash).

1. Atajo para consultar las bases. Usa la contraseña que ya tiene el contenedor; el `-I` es necesario para el `UPDATE` del paso 5.

```bash
sql() { docker compose exec -T sqlserver bash -c "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"\$MSSQL_SA_PASSWORD\" -C -I -W -Q \"$1\""; }
```

2. Stock inicial de `SKU-001`:

```bash
sql "SELECT Sku, Disponible FROM InventoryDb.dbo.Stock WHERE Sku = 'SKU-001'"
```

3. Crear un pedido de 5 unidades y esperar a que quede `Confirmed`:

```bash
curl -s -X POST http://localhost:8080/api/orders -H "Content-Type: application/json" \
  -d '{"nombreCliente":"Idempotencia","sku":"SKU-001","cantidad":5}'
```

4. Revisar que el stock bajó 5 y que hay un `ProcessedEvent`:

```bash
sql "SELECT Sku, Disponible FROM InventoryDb.dbo.Stock WHERE Sku = 'SKU-001'"
sql "SELECT EventId, OrderId, Resultado FROM InventoryDb.dbo.ProcessedEvents"
```

5. Simular el duplicado: marcar el último mensaje del Outbox como no publicado. En menos de 2 segundos el relay lo vuelve a publicar con el mismo `EventId`.

```bash
sql "UPDATE OrdersDb.dbo.OutboxMessages SET ProcesadoEn = NULL WHERE Id = (SELECT TOP 1 Id FROM OrdersDb.dbo.OutboxMessages ORDER BY OcurridoEn DESC)"
```

6. Ver los logs:

```bash
docker compose logs --tail 20 inventory-worker orders-api
```

El worker muestra `Pedido … : Reserved (duplicado, se republica)` y la API `StockReserved ignorado para el pedido …: No se puede confirmar un pedido en estado Confirmed.`

7. Repetir el paso 4: el stock no cambió, sigue habiendo un solo `ProcessedEvent` para ese pedido y el pedido sigue `Confirmed`.

---

## ⚖️ Trade-offs Asumidos

- **Seed duplicado:** los mismos SKU están en `01-orders.sql` y `02-inventory.sql`, y hay que mantenerlos iguales a mano.
- **El catálogo de Orders no tiene el stock real:** solo sirve para validar que el SKU exista; el stock lo conoce Inventory.
- **At-least-once:** el Outbox puede publicar un mensaje dos veces (por ejemplo, si cae entre publicar y marcar como enviado). Se acepta porque los consumidores son idempotentes.
- **Sin timeout de `Pending`:** si un mensaje termina en la DLQ, el pedido se queda `Pending`. Se puede ver en la DLQ, pero nada lo resuelve solo.
- **Paginación en el cliente:** la tabla pagina los 50 pedidos más recientes que devuelve la API.
- **Valores por defecto en el compose:** permiten levantar el sistema sin pasos manuales, pero son credenciales de ejemplo, solo para uso local.

---

## 🔭 Qué Haría Distinto con Más Tiempo

- SignalR para actualizar el panel en lugar de polling.
- Outbox también en Inventory, para que la respuesta no dependa de que el mensaje original siga sin ack.
- Un job que pase a `Rejected` los pedidos que lleven demasiado tiempo en `Pending`.
- Inbox pattern en Orders para registrar los eventos recibidos.
- Paginación en el servidor (por cursor sobre `CreadoEn`).
- Testcontainers con SQL Server y RabbitMQ reales, para probar dos reservas simultáneas del mismo SKU.
- Un Shared Kernel para `Result`, `Error` y `QueueConsumer`, que hoy están duplicados en cada servicio.
- OpenTelemetry para seguir un pedido de la API al worker y de vuelta.
- Manifiestos de Kubernetes (Deployment, Service y ConfigMap) con liveness y readiness sobre los health checks que ya expone la API, límites de recursos y KEDA para escalar el worker según la cola.

---

## 🙋 Autor

### Yeimer Andres Jaramillo Fernandez
📧 Mail: Andresjara0897@hotmail.com <br/>
💼 GitHub: https://github.com/Jaferye97 <br/>
🔗 LinkedIn: https://www.linkedin.com/in/yeimerjarafer/ <br/>
