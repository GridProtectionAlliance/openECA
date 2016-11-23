function [connection_matrix] = system_345KV_connection_analysis_IEEE_118(line_bus_info_345KV, Bus_number_set_345KV)

bus_num = size(Bus_number_set_345KV,1);
connection_matrix =[];

for i=1:size(line_bus_info_345KV,1)
    temp_line_bus_info = line_bus_info_345KV(i,:);
    idx_from = find(Bus_number_set_345KV(:,1)==temp_line_bus_info(1,2));
    idx_to = find(Bus_number_set_345KV(:,1)==temp_line_bus_info(1,3));
    connection_matrix(idx_from,idx_to)=1;
    connection_matrix(idx_to,idx_from)=1;
end

end