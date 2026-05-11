# -*- coding: utf-8 -*-
"""Обновляет подписи к рисункам и названия таблиц в docx."""
import shutil
import xml.etree.ElementTree as ET
import zipfile

W = "{http://schemas.openxmlformats.org/wordprocessingml/2006/main}"
XML_NS = "http://www.w3.org/XML/1998/namespace"

FIGURE_REPLACEMENTS = {
    "Рисунок 1 диаграмма Use-Case. 1 часть": "Рисунок 1 – Диаграмма вариантов использования (Use Case), часть 1",
    "Рисунок 2 диаграмма Use-Case. 2 часть": "Рисунок 2 – Диаграмма вариантов использования (Use Case), часть 2",
    "Рисунок 3 ER-диаграмма": "Рисунок 3 – ER-диаграмма (инфологическая модель данных)",
    "Рисунок 4 схема алгоритма": "Рисунок 4 – Блок-схема алгоритма документооборота (внутренний режим)",
    "Рисунок 5 Диаграмма классов. API": "Рисунок 5 – Диаграмма классов: слой API",
    "Рисунок 6 Диаграмма классов. Logic": "Рисунок 6 – Диаграмма классов: слой Logic",
    "Рисунок 7 Диаграмма классов. Storage": "Рисунок 7 – Диаграмма классов: слой Storage",
    "Рисунок 8 Диаграмма классов. Contracts: StorageContracts + BindingModels": (
        "Рисунок 8 – Диаграмма классов: Contracts (StorageContracts, BindingModels)"
    ),
    "Рисунок 9 Диаграмма классов. Contracts: ViewModels + SearchModels": (
        "Рисунок 9 – Диаграмма классов: Contracts (ViewModels, SearchModels)"
    ),
    "Рисунок 10 Диаграмма классов. Contracts: LogicContracts": "Рисунок 10 – Диаграмма классов: Contracts (LogicContracts)",
    "Рисунок 11 Диаграмма классов. Auth": "Рисунок 11 – Диаграмма классов: слой Auth",
    "Рисунок 12 Диаграмма классов. Models": "Рисунок 12 – Диаграмма классов: слой Models",
    "Рисунок 13 Диаграмма классов. Consumers": "Рисунок 13 – Диаграмма классов: Consumers",
    "Рисунок 14 Диаграмма классов. Мобильное приложение": "Рисунок 14 – Диаграмма классов: мобильное приложение",
    "Рисунок 15 Полная диаграмма классов": "Рисунок 15 – Полная диаграмма классов",
    "Рисунок 16 диаграмма компонентов": "Рисунок 16 – Диаграмма компонентов",
    "Рисунок 17 диаграмма развертывания": "Рисунок 17 – Диаграмма развёртывания",
}

TABLE_LINE_TO_TITLE = {
    "Document": "Таблица 1 – Реквизитный состав таблицы Document",
    "User": "Таблица 2 – Реквизитный состав таблицы User",
    "Role": "Таблица 3 – Реквизитный состав таблицы Role",
    "Document_User (связь “документ-пользователь”)": "Таблица 4 – Реквизитный состав таблицы Document_User",
    'Document_User (связь "документ-пользователь")': "Таблица 4 – Реквизитный состав таблицы Document_User",
    "Signature": "Таблица 5 – Реквизитный состав таблицы Signature",
    "Certificate": "Таблица 6 – Реквизитный состав таблицы Certificate",
}


def para_text(p):
    parts = []
    for t in p.iter(W + "t"):
        if t.text:
            parts.append(t.text)
        if t.tail:
            parts.append(t.tail)
    return "".join(parts)


def set_para_centered_text(p, new_text):
    """Оставляет/добавляет pPr с выравниванием по центру; текст — одним run."""
    ppr = p.find(W + "pPr")
    if ppr is None:
        ppr = ET.Element(W + "pPr")
        p.insert(0, ppr)
    if ppr.find(W + "jc") is None:
        ET.SubElement(ppr, W + "jc").set(W + "val", "center")
    else:
        ppr.find(W + "jc").set(W + "val", "center")
    for child in list(p):
        if child.tag != W + "pPr":
            p.remove(child)
    r = ET.SubElement(p, W + "r")
    t = ET.SubElement(r, W + "t")
    t.set("{%s}space" % XML_NS, "preserve")
    t.text = new_text


def insert_centered_paragraph_before(body, target_el, text):
    idx = list(body).index(target_el)
    p = ET.Element(W + "p")
    ppr = ET.SubElement(p, W + "pPr")
    ET.SubElement(ppr, W + "jc").set(W + "val", "center")
    r = ET.SubElement(p, W + "r")
    t = ET.SubElement(r, W + "t")
    t.set("{%s}space" % XML_NS, "preserve")
    t.text = text
    body.insert(idx, p)


def process(root):
    body = root.find(W + "body")
    # Рисунки
    for p in body.iter(W + "p"):
        txt = para_text(p).strip()
        if txt in FIGURE_REPLACEMENTS:
            set_para_centered_text(p, FIGURE_REPLACEMENTS[txt])

    # Таблицы: несколько проходов insert меняет индексы — обходим с конца по списку tbl
    for tbl in reversed(body.findall(W + "tbl")):
        idx = list(body).index(tbl)
        if idx == 0:
            continue
        prev = body[idx - 1]
        if prev.tag != W + "p":
            continue
        prev_txt = para_text(prev).strip()
        if prev_txt in TABLE_LINE_TO_TITLE:
            set_para_centered_text(prev, TABLE_LINE_TO_TITLE[prev_txt])
        elif prev_txt == "Серверная часть (Backend)":
            insert_centered_paragraph_before(body, tbl, "Таблица 7 – Перечень технологий серверной части (Backend)")
        elif prev_txt == "Клиентская часть (Frontend)":
            insert_centered_paragraph_before(body, tbl, "Таблица 8 – Перечень технологий клиентской части (Frontend)")


def main():
    src = r"c:\Users\1\source\repos\NewDiplom\Насыров_ПИбд-43_2_глава.docx"
    backup = r"c:\Users\1\source\repos\NewDiplom\Насыров_ПИбд-43_2_глава.before_captions.bak.docx"
    shutil.copy2(src, backup)

    with zipfile.ZipFile(src, "r") as zin:
        raw = zin.read("word/document.xml")

    root = ET.fromstring(raw)
    process(root)

    # Точное имя тега с namespace из исходника
    new_xml = raw[:0]  # noqa
    new_xml = ET.tostring(root, encoding="utf-8", xml_declaration=True)

    with zipfile.ZipFile(src, "r") as zin:
        out_buf = []
        for info in zin.infolist():
            data = zin.read(info.filename)
            if info.filename == "word/document.xml":
                data = new_xml
            out_buf.append((info, data))

    with zipfile.ZipFile(src, "w", zipfile.ZIP_DEFLATED) as zout:
        for info, data in out_buf:
            zout.writestr(info, data)

    print("Готово:", src)
    print("Резервная копия:", backup)


if __name__ == "__main__":
    main()
