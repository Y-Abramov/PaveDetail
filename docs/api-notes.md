# PaveDetail - заметки по API Robur

Факты, на которые опирается `Cad/SiteReader.cs`. Всё ниже подтверждено либо
компиляцией против SDK 16.0.62.12, либо тремя живыми прогонами зонда на
проекте юзера 2026-08-27 (зонд с тех пор удалён - его результат воплощён
в `SiteReader`/`SmdxClassifier`).

## Точка входа к площадке

```csharp
PluginCoreOps.FilterOpenedModels((Predicate<IProjectModel>)delegate (IProjectModel pm)
{
    var sm = pm.LockRead() as Topomatic.Sites.Core.SiteModel;
    if (sm != null && sm.Site != null) { /* sm.Site : Site */ }
    return false;
});
```

Тот же путь, что ModelDesk использует для дороги (`ModelView/RoadModelAccess.cs`).
Подтверждено живьём - модель `SiteModel` находится сразу.

**Тупик, не повторять:** `ActiveAlignmentReciver<T>` требует `T : Alignment`,
`SiteModel` наследует `StateControllerObject` - не компилируется (CS0311).
`ActiveProjectReciver` не существует (CS0234).

`Site.Collection : SiteObjectCollection` не `IEnumerable` - обход только индексом.

## Слои дорожной одежды - ДВА независимых источника в одном проекте

Живой прогон 2026-08-27 показал: в Robur есть два разных инструмента задания
состава дорожной одежды, оба пишут данные в одном и том же текстовом формате
`Topomatic.Sites.MaterialsLayers` (`"{толщина}|{id};{толщина}|{id};..."`,
`|`/`;` - фиксированные разделители, толщина - float в текущей культуре потока),
но в разных местах и с разным содержимым `id`:

| Источник | Где хранится | Что в `Id` |
|---|---|---|
| Модификатор **«Слои ДО»** | Семантика ОДНОГО из слоёв `SitePavement`, привязанного к региону через `BasePatch == Region.Patch`. Тег/имя нестабильны (в тесте - голый числовой ключ 15, без читаемого имени) | Код классификатора, например `SmdxGenplanSmallAsphalt` |
| Семантическое поле **«Контроллер КДО»** (`SiteSimpleObject.Semantic`, тег `MaterialsLayers`) | Прямо в семантике региона | Готовое русское имя материала, например `Асфальтобетон мелкозернистый` |

Оба источника независимы: заполнение одного не гарантирует заполнение
другого (в тесте юзера «Контроллер КДО» оказался пустой строкой уже после
того, как модификатор «Слои ДО» реально создал `SitePavement`).

**`SiteReader.FindMaterialsLayersString` ищет строку по содержимому**
(регэксп `^\d+([.,]\d+)?\|[^;|]+(;...)*$`), а не по конкретному тегу - это
переживёт расхождения тегов между версиями/продуктами. Порядок проверки:
сначала слои `SitePavement` (это реально построенная 3D-конструкция),
потом семантика региона (черновая описательная заметка).

`SitePavement.Layers` может содержать лишние слои, НЕ относящиеся к составу:
в тесте `Layers.Count == 3`, но состав (все 6 материалов сразу) лежал только
в `Layers[0]`; `Layers[1]`/`Layers[2]` несли визуализационные пресеты для
3D-солида (`"Площадки из ЩПС (ПГС) Тип 1"`, шейдер-блобы в base64,
`LinearVisualizationTemplate`) - к альбому узлов отношения не имеют.
Поиск по регэкспу по всем слоям сам их пропускает (их значения не матчатся).

## Классификатор материалов (SmdxGenplan*)

Коды вида `SmdxGenplanSmallAsphalt` резолвятся в русские имена через JSON-файл:

```
%ProgramData%\Topomatic\{Продукт}\{Версия}\Support\Smdx\materials.smdx
```

(тот же паттерн вычисления продукта/версии из имени папки установки, что
`Shared\Bootstrap\TpmInstaller.FindPackagesJson` - см. `Cad/SmdxClassifierLocator.cs`).

Формат - плоский JSON-массив `{ "id": "...", "name": "...", "parent": "..." }`,
75 записей в SDK 16.0.62.12 продукта Road, полностью покрывает домен площадок:
асфальтобетон/бетон/геосинтетика/грунт/камень/песок/плитка/резина/связующее/
сетка/смеси. Файл идентичен между продуктами Road/Genplan/Culverts/Pipes/edu
(проверено наличие, не байт-в-байт).

`SmdxClassifier.Resolve(idOrText)` - код найден в словаре -> имя, не найден ->
возвращает вход как есть. Это единственный резолвер, нужный для ОБОИХ
источников: коды `SitePavement` резолвятся в имена, готовые русские имена из
«Контроллера КДО» просто проходят насквозь без изменений.

## Борта

**ЖДЁТ ДАМПА.** Во всех трёх живых прогонах `SiteBorder` в проекте не было
(юзер не настраивал), `SiteRegion.BaseLine` пустой. Гипотеза - совпадение
`BaseLine` региона и борта - не проверена. `SiteReader.FindBorder` уже
защищён от пустых строк (не сопоставляет `""` с `""`), но реальное
совпадающее значение никогда не наблюдалось. Закрывается в Task 14
(живой гейт), когда у региона появится настоящий борт.

## Примитивы чертежа (DwgEntity)

- `DwgText.Content` - не `.Text`. `Height`, `Position : Vector3D`.
- `DwgHatch.PatternName/.PatternScale/.PatternAngle`; контур -
  `hatch.BoundaryPath.Add(new PolylineBoundaryPath { ... })`, точки -
  `path.Add(new BugleVector2D(vector2d))`, `path.IsClosed = true`.
- `DwgPolyline.Add(new BugleVector2D(...))` (не `AddVertex`), `.Closed`.
- Слой примитиву - `entity.Layer = dwgLayer`; коллекция слоёв -
  `Drawing.Layers.IsExists(name)`/`.Add(name)`, индексатор `Layers[name]`.
- `drawing.ActiveSpace.Add(entity)`, транзакция `BeginUpdate()/EndUpdate()`
  без аргументов в этой версии SDK.
