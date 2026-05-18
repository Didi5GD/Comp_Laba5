Лабораторная работа 5. Разработка синтаксического анализатора (парсера)
Автор: Горащенко Дарья Романовна, факультет АВТФ,курс 3, семестр 6, группа АВТ-313.

Вариант задания: 94. Условный оператор if-else на языке PHP

Примеры кода корректные (многострочные в том числе) 
 if ($a > $b) { $max = $a; } else { $max = $b; };


if($a>$b){ $a--;} else{ $a=$b; };

Контекстно-зависимые условия:
надо чтобы хотябы один идинтификатор из блока условий находился в блоке выполнения if или выполнения else

if ($a > $b) { $max = $a; } else { $max = $b; };
0 ошибок
<img width="547" height="793" alt="image" src="https://github.com/user-attachments/assets/73469748-8f1c-40f6-9b01-06e5033fc962" />
<img width="896" height="581" alt="image" src="https://github.com/user-attachments/assets/0c67c91d-bd45-49d7-bae8-f862a97c03a1" />


if ($a > $l) { $max = $a; } else { $max = $b; };
1 ошибка
<img width="535" height="826" alt="image" src="https://github.com/user-attachments/assets/a7c3c603-79d8-4c95-a234-4ace91daafb2" />
<img width="897" height="588" alt="image" src="https://github.com/user-attachments/assets/2f0421c6-0955-4445-9741-8384bf7c1ceb" />


if ($r > $l) { $max = $a; } else { $max = $b; };
2 ошибки
<img width="481" height="803" alt="image" src="https://github.com/user-attachments/assets/5d12ab61-6a9e-4afe-bf20-3adfeca34d01" />
<img width="896" height="585" alt="image" src="https://github.com/user-attachments/assets/963c9d69-f982-4240-81d4-bb385f52fb80" />

