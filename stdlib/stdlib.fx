int print(string value, int length) 
{
    return syscall(1, 1, value, length); 
}

ptr alloc(int length) 
{   
    ptr currentPointer = syscall(12, 0) as ptr;
    ptr movedPointer = syscall(12, currentPointer + length) as ptr;
    return currentPointer;
}

string scan(int length) 
{
    ptr pointer = alloc(length);
    syscall(0, 0, pointer, length);
    return pointer as string;
}

int strlen(string value) 
{
    int i = 0;
    while (true) 
    {
        if (peek(value, i) == 0)
        {
            break;
        }
        else
        {
            i = i + 1;
        }
    }
    return i;
}

//int intToString(int value, string out) 
//{
//    return stringLen; 
//}
