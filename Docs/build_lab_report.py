# -*- coding: utf-8 -*-
"""Сборка отчёта по практическим занятиям №7 и №8 в формате DOCX."""

from docx import Document
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Cm, Pt, RGBColor

OUT = r"C:\Addons\Work\Unity\Projects\ILiII-2026-09-22_18-19-31\Docs\Lab7_Lab8_Report.docx"

ACCENT = RGBColor(0x2E, 0x4A, 0x7A)
MUTED = RGBColor(0x55, 0x5F, 0x70)


def style_document(document):
    normal = document.styles["Normal"]
    normal.font.name = "Calibri"
    normal.font.size = Pt(11)
    normal.paragraph_format.space_after = Pt(6)
    normal.paragraph_format.line_spacing = 1.15

    for name, size, color in (("Heading 1", 16, ACCENT), ("Heading 2", 13, ACCENT)):
        style = document.styles[name]
        style.font.name = "Calibri"
        style.font.size = Pt(size)
        style.font.bold = True
        style.font.color.rgb = color
        style.paragraph_format.space_before = Pt(14)
        style.paragraph_format.space_after = Pt(6)

    for section in document.sections:
        section.left_margin = Cm(2.2)
        section.right_margin = Cm(2.0)
        section.top_margin = Cm(2.0)
        section.bottom_margin = Cm(2.0)


def add_table(document, headers, rows, widths=None):
    table = document.add_table(rows=1, cols=len(headers))
    table.style = "Table Grid"
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = False

    header_cells = table.rows[0].cells
    for index, text in enumerate(headers):
        header_cells[index].text = ""
        paragraph = header_cells[index].paragraphs[0]
        run = paragraph.add_run(text)
        run.bold = True
        run.font.size = Pt(10)

    for row in rows:
        cells = table.add_row().cells
        for index, text in enumerate(row):
            cells[index].text = ""
            paragraph = cells[index].paragraphs[0]
            run = paragraph.add_run(str(text))
            run.font.size = Pt(10)

    # Ширины столбцов: фиксируем и в сетке таблицы, и в ячейках,
    # иначе LibreOffice/Word растягивают столбцы поровну.
    if widths:
        for index, width in enumerate(widths):
            table.columns[index].width = Cm(width)
        for row in table.rows:
            for index, width in enumerate(widths):
                row.cells[index].width = Cm(width)

    # Заголовок повторяется на каждой странице, строки не рвутся пополам.
    header_tr = table.rows[0]._tr
    header_trPr = header_tr.get_or_add_trPr()

    tbl_header = OxmlElement("w:tblHeader")
    tbl_header.set(qn("w:val"), "true")
    header_trPr.append(tbl_header)

    for row in table.rows:
        trPr = row._tr.get_or_add_trPr()
        trPr.append(OxmlElement("w:cantSplit"))

    document.add_paragraph()
    return table


def bullets(document, items):
    for item in items:
        document.add_paragraph(item, style="List Bullet")


def code(document, text):
    paragraph = document.add_paragraph()
    paragraph.paragraph_format.left_indent = Cm(0.8)
    run = paragraph.add_run(text)
    run.font.name = "Consolas"
    run.font.size = Pt(9)
    run.font.color.rgb = MUTED


def main():
    document = Document()
    style_document(document)

    # ── Титульный блок ───────────────────────────────────────────
    title = document.add_paragraph()
    title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    run = title.add_run("Практические занятия №7 и №8")
    run.font.size = Pt(24)
    run.bold = True
    run.font.color.rgb = ACCENT

    subtitle = document.add_paragraph()
    subtitle.alignment = WD_ALIGN_PARAGRAPH.CENTER
    run = subtitle.add_run("Игровая логика и искусственный интеллект")
    run.font.size = Pt(14)
    run.font.color.rgb = MUTED

    project = document.add_paragraph()
    project.alignment = WD_ALIGN_PARAGRAPH.CENTER
    run = project.add_run("Отчёт о выполнении\nИгровой инвентарь персонажа и ИИ с хаотичной моделью поведения")
    run.font.size = Pt(12)

    document.add_paragraph()
    add_table(
        document,
        ["Параметр", "Значение"],
        [
            ["Движок", "Unity 6000.5.9f1 (URP)"],
            ["Проект", "ILiII-2026-09-22_18-19-31"],
            ["Демо-сцена", "Assets/Scenes/Lab7_8_Demo.unity"],
            ["Язык", "C# (сборка LabGame)"],
            ["Автотесты", "36 EditMode + 12 PlayMode — все проходят"],
        ],
        widths=[5.0, 10.5],
    )

    # ── 1. Постановка задачи ─────────────────────────────────────
    document.add_heading("1. Постановка задачи", level=1)

    document.add_heading("Практическое занятие №7", level=2)
    document.add_paragraph(
        "Разработать игровой инвентарь персонажа и реализовать механики: "
        "перемещение предметов внутри инвентаря, экипировку предмета, "
        "выбрасывание предмета в мир и подбор предмета. "
        "Дополнительное задание — изменение характеристик персонажа при экипировке."
    )

    document.add_heading("Практическое занятие №8", level=2)
    document.add_paragraph(
        "Спроектировать ИИ с хаотичной моделью поведения: НПС выполняет непредсказуемые "
        "действия, запутывает игрока и реагирует на изменения игрового процесса."
    )

    # ── 2. ПЗ №7 ─────────────────────────────────────────────────
    document.add_heading("2. Практическое занятие №7: инвентарь", level=1)

    document.add_heading("2.1. Соответствие требованиям", level=2)
    add_table(
        document,
        ["Требование", "Реализация"],
        [
            ["1. Перемещение предметов", "InventoryModel.Move; drag & drop в InventoryUI / InventorySlotUI"],
            ["2. Экипировка предмета", "InventoryModel.Equip / EquipTo; слоты EquipmentSlotUI"],
            ["3. Выбрасывание предмета", "InventoryModel.Take / TakeEquipped; PlayerInventory.DropSlot; WorldItemSpawner"],
            ["4. Подбор предмета", "ItemPickup + PlayerInteractor + PlayerInventory.TryPickUp"],
            ["Доп. задание: характеристики", "CharacterStats + StatCalculator + PlayerSpeedLink"],
        ],
        widths=[5.5, 10.0],
    )

    document.add_heading("2.2. Архитектура", level=2)
    document.add_paragraph(
        "Логика инвентаря отделена от Unity-сцены: InventoryModel — обычный C#-класс, "
        "который не знает ни про UI, ни про ввод. Это позволило полностью покрыть "
        "правила юнит-тестами и упростило отладку."
    )
    add_table(
        document,
        ["Файл", "Назначение"],
        [
            ["Items/ItemDefinition.cs", "Описание предмета (ScriptableObject): слот, стаки, вес, бонусы"],
            ["Items/ItemStack.cs", "Стопка предметов в слоте"],
            ["Items/StatBlock.cs", "Характеристики, StatType, StatModifier"],
            ["Items/StatCalculator.cs", "Пересчёт характеристик с учётом экипировки"],
            ["Inventory/InventoryModel.cs", "Правила: слоты, экипировка, перемещение, добавление, изъятие"],
            ["Inventory/CharacterStats.cs", "MonoBehaviour: база + бонусы, события изменений"],
            ["Inventory/PlayerSpeedLink.cs", "Переносит скорость персонажа в PhysicsModule.maxSpeed"],
            ["Interaction/*", "Подбор: IInteractable, реестр, интерактор игрока, предмет в мире"],
            ["UI/*", "Экран инвентаря, слоты, тултип, HUD — строятся кодом"],
            ["PlayerInventory.cs", "Компонент на игроке: связывает модель, мир и интерфейс"],
        ],
        widths=[5.8, 9.7],
    )

    document.add_heading("2.3. Механики", level=2)
    document.add_paragraph(
        "Инвентарь состоит из 24 слотов сетки и 8 слотов экипировки "
        "(голова, броня, перчатки, обувь, оружие, щит, амулет). "
        "Модель поднимает события Changed, ItemAdded, ItemRemoved, ItemEquipped и "
        "ItemUnequipped — на них подписаны интерфейс и характеристики, поэтому "
        "все цифры обновляются автоматически."
    )
    bullets(
        document,
        [
            "Перемещение: перетаскивание мышью. Пустой слот — перенос, тот же предмет — "
            "объединение стопок, другой предмет — обмен местами.",
            "Экипировка: перетаскивание на слот экипировки или двойной клик. "
            "Ранее надетый предмет возвращается в освободившийся слот.",
            "Выбрасывание: клавиша G или кнопка. Предмет появляется перед игроком, "
            "прижимается к земле и снова становится доступным для подбора.",
            "Подбор: клавиша E рядом с предметом. Подбор атомарный — если предмет "
            "не помещается целиком, он остаётся в мире.",
        ],
    )

    document.add_heading("2.4. Изменение характеристик при экипировке", level=2)
    document.add_paragraph("Формула пересчёта:")
    code(
        document,
        "итог = (база + Σ flat) × (1 + Σ percent)\n"
        "итог = (10 + 12) × 1.00 = 22 атаки  — пример для стального меча",
    )
    document.add_paragraph(
        "Проценты складываются, а не перемножаются, чтобы результат был предсказуем "
        "для игрока. Значения защищены от отрицательных величин: HP не ниже 1, "
        "скорость не ниже 0.1, крит в диапазоне 0…1. "
        "PlayerSpeedLink переносит итоговую скорость в PhysicsModule.maxSpeed, "
        "поэтому эффект экипировки виден не только в цифрах, но и в движении."
    )
    add_table(
        document,
        ["Предмет", "Слот", "Эффект"],
        [
            ["Стальной меч", "Оружие", "+12 атака, −0.5 скорость, +5 % крит"],
            ["Дубовый щит", "Щит", "+6 защита, −0.3 скорость"],
            ["Железный шлем", "Голова", "+4 защита, +15 HP"],
            ["Кожаная куртка", "Броня", "+3 защита, +7 % скорость"],
            ["Сапоги скорости", "Обувь", "+1.2 скорость"],
            ["Амулет силы", "Амулет", "+5 атака, +5 % крит"],
        ],
        widths=[5.0, 3.0, 7.5],
    )

    # ── 3. ПЗ №8 ─────────────────────────────────────────────────
    document.add_heading("3. Практическое занятие №8: хаотичный ИИ", level=1)

    document.add_heading("3.1. Подход", level=2)
    document.add_paragraph(
        "Классический конечный автомат («патруль → погоня → возврат») предсказуем: "
        "игрок быстро вычисляет шаблон. В работе вместо жёстких переходов используется "
        "взвешенный случайный выбор действий с памятью и реакциями на события."
    )
    document.add_paragraph("Непредсказуемость обеспечивают четыре механизма:")
    bullets(
        document,
        [
            "взвешенный выбор из девяти действий, веса зависят от ситуации;",
            "анти-повтор: недавние действия штрафуются экспоненциально, поэтому NPC не зацикливается;",
            "случайная длительность каждого действия;",
            "реакции: события игры смещают веса и с заданной вероятностью мгновенно меняют действие.",
        ],
    )

    document.add_heading("3.2. Набор действий", level=2)
    add_table(
        document,
        ["Действие", "Поведение", "Что запутывает игрока"],
        [
            ["Wander", "бродит вокруг точки появления", "базовое состояние, «мирный» NPC"],
            ["Stalk", "крадётся, держит дистанцию, замирает под взглядом", "слежка без агрессии"],
            ["Flee", "убегает", "контраст с предыдущим действием"],
            ["Ambush", "обходит со спины, ждёт и бросается", "атака из неожиданного места"],
            ["Mimic", "повторяет движение игрока", "«он копирует меня»"],
            ["Taunt", "подбегает, дразнит, убегает", "ложная агрессия"],
            ["FakeGift", "подкидывает предмет-обманку", "подбор предмета наказывается"],
            ["Freeze", "замирает и смотрит", "классический «странный NPC»"],
            ["Blink", "исчезает, пока на него не смотрят", "«он был здесь секунду назад»"],
        ],
        widths=[2.6, 7.2, 5.7],
    )

    document.add_heading("3.3. Реакции на изменения игрового процесса", level=2)
    document.add_paragraph(
        "События публикуются через статическую шину GameEventBus. Её поднимают "
        "PlayerInventory (подбор, экипировка, выбрасывание), CharacterStats (здоровье), "
        "NPCController (состояние квеста) и ItemPickup (обманка)."
    )
    add_table(
        document,
        ["Сигнал", "Реакция NPC"],
        [
            ["PlayerPickedUpItem", "Stalk или FakeGift"],
            ["PlayerEquippedItem", "Flee или Ambush"],
            ["PlayerDroppedItem", "FakeGift или Taunt"],
            ["PlayerDamaged", "Taunt или Ambush"],
            ["QuestChanged", "Taunt или Blink"],
            ["PlayerLookedAtMe", "Freeze или Flee"],
            ["PlayerCameClose", "Ambush или Mimic"],
        ],
        widths=[6.0, 9.5],
    )
    document.add_paragraph(
        "Кроме событий, NPC сам отслеживает переходы: «игрок посмотрел», «игрок подошёл», "
        "урон по здоровью и смену состояния квеста. Модуль NpcPerception определяет "
        "расстояние, прямую видимость и — главное — смотрит ли игрок на NPC "
        "(угол между направлением камеры и направлением на NPC). Именно это делает "
        "поведение «Замер» и «Исчезновение» убедительным."
    )

    document.add_heading("3.4. Архитектура ИИ", level=2)
    add_table(
        document,
        ["Файл", "Назначение"],
        [
            ["AI/ChaosTypes.cs", "Действия, сигналы, снимок ситуации (ChaosContext)"],
            ["AI/ChaosProfile.cs", "ScriptableObject: хаотичность, вероятности, реплики"],
            ["AI/IChaosActor.cs", "Абстракция NPC — позволяет тестировать мозг без сцены"],
            ["AI/IChaosAction.cs", "Интерфейс действия (паттерн «Стратегия»)"],
            ["AI/ChaosActions.cs", "Девять реализаций действий и фабрика"],
            ["AI/ChaosBrain.cs", "Взвешенный выбор, анти-повтор, реакции"],
            ["AI/ChaosAgent.cs", "Мост к NavMeshAgent и Animator, телепорт, спавн обманок"],
            ["AI/NpcPerception.cs", "Зрение и определение «взгляда игрока»"],
            ["AI/ChaoticNPC.cs", "Драйвер: подписка на события, HUD, отладочный вывод"],
        ],
        widths=[5.0, 10.5],
    )

    # ── 4. Тестирование ──────────────────────────────────────────
    document.add_heading("4. Тестирование", level=1)
    document.add_paragraph(
        "Для проверки механик написаны автоматические тесты. "
        "Логика инвентаря и «мозга» NPC не зависит от сцены, поэтому основная часть "
        "проверок выполняется в режиме EditMode, а взаимодействие с Unity-компонентами — "
        "в режиме PlayMode."
    )
    add_table(
        document,
        ["Набор", "Тестов", "Результат", "Что проверяется"],
        [
            ["EditMode / InventoryModel", "20", "пройдено",
             "добавление, стаки, переполнение, атомарный подбор, перемещение, обмен, "
             "слияние, экипировка, возврат предмета, снятие, изъятие, события, вес"],
            ["EditMode / StatCalculator", "6", "пройдено",
             "формула flat/percent, суммирование бонусов, защита от отрицательных значений"],
            ["EditMode / ChaosBrain", "10", "пройдено",
             "используются все 9 действий, отсутствие повторов и доминирования, "
             "реакции, детерминированность по seed"],
            ["PlayMode / Inventory", "8", "пройдено",
             "подбор, полный инвентарь, экипировка и рост характеристик, "
             "влияние на PhysicsModule.maxSpeed, снятие, выбрасывание и повторный подбор, "
             "сборка интерфейса"],
            ["PlayMode / Chaos NPC", "4", "пройдено",
             "создание «мозга», реакция на подбор предмета и на урон, работа по кадрам"],
        ],
        widths=[4.0, 1.8, 2.2, 7.5],
    )
    document.add_paragraph("Итог: 48 автоматических тестов, все проходят.")
    document.add_paragraph("Запуск в редакторе: Window → General → Test Runner → Run All.")
    document.add_paragraph("Запуск из консоли:")
    code(
        document,
        "Unity.exe -batchmode -projectPath <проект> -runTests \\\n"
        "  -testPlatform PlayMode -testResults Logs\\playmode-results.xml",
    )

    # ── 5. Запуск демо ───────────────────────────────────────────
    document.add_heading("5. Запуск демонстрации", level=1)
    document.add_paragraph(
        "Открыть сцену Assets/Scenes/Lab7_8_Demo.unity и нажать Play. "
        "Сцена создана как копия рабочей SampleScene, поэтому исходная сцена не изменена. "
        "Пересобрать её можно через меню Lab 7-8 → 1. Создать демо-сцену."
    )
    add_table(
        document,
        ["Клавиша", "Действие"],
        [
            ["WASD", "движение персонажа"],
            ["Tab", "открыть / закрыть инвентарь"],
            ["ЛКМ + перетаскивание", "перемещение предмета между слотами"],
            ["Перетаскивание на слот экипировки", "надеть предмет"],
            ["Двойной клик", "надеть / снять предмет"],
            ["G", "выбросить выделенный предмет"],
            ["E", "подобрать предмет / поговорить с NPC"],
            ["F", "сдать или отменить квест"],
            ["ESC", "закрыть инвентарь (вне инвентаря — выход)"],
        ],
        widths=[6.0, 9.5],
    )
    document.add_paragraph(
        "На земле рядом с игроком лежат девять предметов: меч, щит, шлем, куртка, "
        "сапоги, амулет, два набора зелий и ягоды. Хаотичный NPC находится рядом "
        "с квестовым NPC; в левом верхнем углу экрана отображается его текущее действие."
    )

    # ── 6. Выводы ────────────────────────────────────────────────
    document.add_heading("6. Выводы", level=1)
    bullets(
        document,
        [
            "Реализован инвентарь с перемещением, экипировкой, выбрасыванием и подбором предметов; "
            "логика отделена от представления и покрыта тестами.",
            "Дополнительное задание выполнено: экипировка меняет характеристики персонажа, "
            "причём изменение влияет и на интерфейс, и на реальную скорость движения.",
            "Спроектирован ИИ с хаотичной моделью поведения: девять действий, анти-повтор, "
            "случайные длительности и семь типов реакций на события игры.",
            "Непредсказуемость подтверждена измерением: тесты проверяют, что используются все "
            "действия, нет доминирующего действия и почти нет мгновенных повторов.",
            "Все 48 автоматических тестов проходят в batch-режиме Unity.",
        ],
    )

    document.save(OUT)
    print("SAVED", OUT)


if __name__ == "__main__":
    main()
