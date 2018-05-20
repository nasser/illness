CC=gcc
CFLAGS=-O2 `pkg-config --cflags glib-2.0` `pkg-config --cflags mono-2` -Iinclude
LDFLAGS=`pkg-config --libs glib-2.0` -Wl,-undefined,dynamic_lookup
# /usr/local/Cellar/mono/5.0.0.100/lib/libmono-2.0.a -framework CoreFoundation -framework Foundation -liconv
# `pkg-config --libs mono-2`
# -Wl,-undefined,dynamic_lookup

all: libillness Illness.exe

Illness.exe: Illness.dll Program.cs
	mcs Program.cs /r:Illness.dll /r:System.Numerics.dll
	
Illness.dll: Illness.cs
	mcs /t:library Illness.cs

libillness: illness.c
	$(CC) $(CFLAGS) -g -c -fPIC illness.c -o illness.o
	$(CC) $(LDFLAGS) -shared illness.o libmonodis.a libZydis.a -o libillness.so

clean:
	rm -rf *.so *.o *.exe *.dll
